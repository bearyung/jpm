using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace JinPingMei.Game.Hosting.Text;

internal sealed class GraphemeBuffer
{
    private static readonly (int Start, int End)[] WideRanges =
    {
        (0x1100, 0x115F),
        (0x2329, 0x232A),
        (0x2E80, 0xA4CF),
        (0xAC00, 0xD7A3),
        (0xF900, 0xFAFF),
        (0xFE10, 0xFE6F),
        (0xFF00, 0xFF60),
        (0xFFE0, 0xFFE6),
        (0x1F300, 0x1F64F),
        (0x1F900, 0x1F9FF),
        (0x1FA70, 0x1FAFF)
    };

    private readonly List<GraphemeCluster> _clusters = new();
    private bool _pendingJoin;
    private int _cursorPosition; // Position in clusters

    public int CursorPosition => _cursorPosition;
    public int Length => _clusters.Count;

    public void Append(Rune rune)
    {
        // Insert at cursor position
        if (_cursorPosition < _clusters.Count)
        {
            // Check if we should combine with the cluster before cursor
            if (_cursorPosition > 0 && ShouldCombine(rune))
            {
                _clusters[_cursorPosition - 1].Add(rune);
                return;
            }

            var newCluster = new GraphemeCluster();
            newCluster.Add(rune);
            _clusters.Insert(_cursorPosition, newCluster);
            _cursorPosition++;
            _pendingJoin = rune.Value == 0x200D;
            return;
        }

        // Append at the end (cursor at end)
        if (_clusters.Count > 0 && ShouldCombine(rune))
        {
            _clusters[^1].Add(rune);
            return;
        }

        var endCluster = new GraphemeCluster();
        endCluster.Add(rune);
        _clusters.Add(endCluster);
        _cursorPosition++;
        _pendingJoin = rune.Value == 0x200D;
    }

    public bool MoveCursorLeft(out int displayDelta)
    {
        if (_cursorPosition > 0)
        {
            _cursorPosition--;
            displayDelta = _clusters[_cursorPosition].DisplayWidth;
            return true;
        }
        displayDelta = 0;
        return false;
    }

    public bool MoveCursorRight(out int displayDelta)
    {
        if (_cursorPosition < _clusters.Count)
        {
            displayDelta = _clusters[_cursorPosition].DisplayWidth;
            _cursorPosition++;
            return true;
        }
        displayDelta = 0;
        return false;
    }

    public void MoveCursorToStart()
    {
        _cursorPosition = 0;
    }

    public void MoveCursorToEnd()
    {
        _cursorPosition = _clusters.Count;
    }

    public string GetTextAfterCursor()
    {
        if (_cursorPosition >= _clusters.Count)
        {
            return string.Empty;
        }

        var remainingClusters = _clusters.Skip(_cursorPosition).ToArray();
        var totalLength = remainingClusters.Sum(c => c.Utf16Length);

        if (totalLength == 0)
        {
            return string.Empty;
        }

        return string.Create(totalLength, remainingClusters, static (span, clusters) =>
        {
            var index = 0;
            foreach (var cluster in clusters)
            {
                foreach (var rune in cluster.Runes)
                {
                    index += rune.EncodeToUtf16(span[index..]);
                }
            }
        });
    }

    public int GetDisplayWidthAfterCursor()
    {
        if (_cursorPosition >= _clusters.Count)
        {
            return 0;
        }

        return _clusters.Skip(_cursorPosition).Sum(c => c.DisplayWidth);
    }

    public bool TryBackspace(out int width)
    {
        if (_cursorPosition == 0)
        {
            width = 0;
            _pendingJoin = false;
            return false;
        }

        var removeIndex = _cursorPosition - 1;
        var removed = _clusters[removeIndex];
        _clusters.RemoveAt(removeIndex);
        _cursorPosition--;
        _pendingJoin = false;
        width = removed.DisplayWidth;
        return true;
    }

    public bool TryDelete(out int width)
    {
        if (_cursorPosition >= _clusters.Count)
        {
            width = 0;
            return false;
        }

        var removed = _clusters[_cursorPosition];
        _clusters.RemoveAt(_cursorPosition);
        width = removed.DisplayWidth;
        return true;
    }

    public bool TryDrain(out string result)
    {
        if (_clusters.Count == 0)
        {
            _pendingJoin = false;
            result = string.Empty;
            return false;
        }

        var totalLength = 0;
        foreach (var cluster in _clusters)
        {
            totalLength += cluster.Utf16Length;
        }

        var snapshot = _clusters.ToArray();
        result = string.Create(totalLength, snapshot, static (span, clusters) =>
        {
            var index = 0;
            foreach (var cluster in clusters)
            {
                foreach (var rune in cluster.Runes)
                {
                    index += rune.EncodeToUtf16(span[index..]);
                }
            }
        });

        _clusters.Clear();
        _pendingJoin = false;
        _cursorPosition = 0;
        return true;
    }

    private bool ShouldCombine(Rune rune)
    {
        if (_pendingJoin)
        {
            _pendingJoin = rune.Value == 0x200D;
            return true;
        }

        var category = Rune.GetUnicodeCategory(rune);
        switch (category)
        {
            case UnicodeCategory.NonSpacingMark:
            case UnicodeCategory.SpacingCombiningMark:
            case UnicodeCategory.EnclosingMark:
                return true;
            case UnicodeCategory.Format:
                if (rune.Value == 0x200D)
                {
                    _pendingJoin = true;
                }
                return true;
        }

        if (rune.Value is >= 0xFE00 and <= 0xFE0F)
        {
            return true;
        }

        if (rune.Value is >= 0xE0100 and <= 0xE01EF)
        {
            return true;
        }

        if (rune.Value is >= 0x1F3FB and <= 0x1F3FF)
        {
            return true;
        }

        return false;
    }

    private static int GetDisplayWidth(Rune rune)
    {
        var category = Rune.GetUnicodeCategory(rune);
        switch (category)
        {
            case UnicodeCategory.NonSpacingMark:
            case UnicodeCategory.SpacingCombiningMark:
            case UnicodeCategory.EnclosingMark:
            case UnicodeCategory.Format:
            case UnicodeCategory.Control:
                return 0;
        }

        var value = rune.Value;

        if (value is >= 0x1F3FB and <= 0x1F3FF)
        {
            return 0;
        }
        foreach (var (start, end) in WideRanges)
        {
            if (value >= start && value <= end)
            {
                return 2;
            }
        }

        return 1;
    }

    private sealed class GraphemeCluster
    {
        public List<Rune> Runes { get; } = new();
        public int DisplayWidth { get; private set; }
        public int Utf16Length { get; private set; }

        public void Add(Rune rune)
        {
            Runes.Add(rune);
            Utf16Length += rune.Utf16SequenceLength;

            // For the first rune, use its display width
            // For subsequent runes (combining marks, modifiers), only add non-zero width
            var width = GetDisplayWidth(rune);
            if (Runes.Count == 1)
            {
                DisplayWidth = width;
            }
            else if (width > 0)
            {
                // Only override display width if this is a non-combining character with width
                // This handles emoji sequences properly
                DisplayWidth = Math.Max(DisplayWidth, width);
            }
        }
    }
}
