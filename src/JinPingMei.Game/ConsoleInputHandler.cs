using System;
using System.Collections.Generic;
using System.Text;
using JinPingMei.Game.Hosting.Text;
using Spectre.Console;

namespace JinPingMei.Game;

/// <summary>
/// Custom console input handler that supports cursor movement, command history, and proper Unicode handling
/// </summary>
internal sealed class ConsoleInputHandler
{
    private readonly GraphemeBuffer _buffer = new();
    private readonly string _prompt;
    private readonly int _promptDisplayWidth;
    private readonly List<string> _commandHistory;
    private int _historyIndex = -1;
    private string _currentInput = string.Empty;

    public ConsoleInputHandler(string prompt, List<string>? commandHistory = null)
    {
        _prompt = prompt;
        // Calculate the actual display width of the prompt (without markup)
        // Need to account for wide characters (Chinese characters = 2 width)
        var plainPrompt = prompt.RemoveMarkup();
        _promptDisplayWidth = CalculateStringDisplayWidth(plainPrompt);
        _commandHistory = commandHistory ?? new List<string>();
        _historyIndex = _commandHistory.Count;
    }

    public string ReadLine()
    {
        // Display initial prompt
        AnsiConsole.Markup(_prompt);

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    // Submit input
                    Console.WriteLine();
                    if (_buffer.TryDrain(out var result))
                    {
                        return result;
                    }
                    return string.Empty;

                case ConsoleKey.Backspace:
                    // Delete character before cursor
                    if (_buffer.TryBackspace(out var backspaceWidth))
                    {
                        // Get current cursor position
                        var currentPos = Console.GetCursorPosition();

                        // Move cursor back to where the deleted character was
                        var newLeft = currentPos.Left - backspaceWidth;
                        Console.SetCursorPosition(newLeft, currentPos.Top);

                        // Get text after cursor (if any)
                        var afterText = _buffer.GetTextAfterCursor();

                        // Write the text after cursor to shift it left
                        if (!string.IsNullOrEmpty(afterText))
                        {
                            Console.Write(afterText);
                        }

                        // Clear the leftover characters at the end
                        for (int i = 0; i < backspaceWidth; i++)
                        {
                            Console.Write(" ");
                        }

                        // Reset cursor to correct position
                        Console.SetCursorPosition(newLeft, currentPos.Top);
                    }
                    break;

                case ConsoleKey.Delete:
                    // Delete character at cursor
                    if (_buffer.TryDelete(out var deleteWidth))
                    {
                        // Redraw text after cursor
                        var afterText = _buffer.GetTextAfterCursor();
                        var currentPos = Console.GetCursorPosition();
                        Console.Write(afterText);

                        // Clear the extra characters at the end
                        for (int i = 0; i < deleteWidth; i++)
                        {
                            Console.Write(" ");
                        }
                        Console.SetCursorPosition(currentPos.Left, currentPos.Top);
                    }
                    break;

                case ConsoleKey.UpArrow:
                    // Navigate to previous command in history
                    if (_commandHistory.Count > 0 && _historyIndex > 0)
                    {
                        // Save current input if we're at the end of history
                        if (_historyIndex == _commandHistory.Count)
                        {
                            _buffer.TryDrain(out _currentInput);
                        }

                        // Move to previous command
                        _historyIndex--;
                        ReplaceCurrentLine(_commandHistory[_historyIndex]);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    // Navigate to next command in history
                    if (_historyIndex < _commandHistory.Count)
                    {
                        _historyIndex++;

                        if (_historyIndex == _commandHistory.Count)
                        {
                            // Restore the current input that was being typed
                            ReplaceCurrentLine(_currentInput);
                        }
                        else
                        {
                            ReplaceCurrentLine(_commandHistory[_historyIndex]);
                        }
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    // Move cursor left
                    if (_buffer.MoveCursorLeft(out var leftWidth))
                    {
                        var currentPos = Console.GetCursorPosition();
                        var newLeft = currentPos.Left - leftWidth;
                        if (newLeft >= _promptDisplayWidth)
                        {
                            Console.SetCursorPosition(newLeft, currentPos.Top);
                        }
                    }
                    break;

                case ConsoleKey.RightArrow:
                    // Move cursor right
                    if (_buffer.MoveCursorRight(out var rightWidth))
                    {
                        var currentPos = Console.GetCursorPosition();
                        Console.SetCursorPosition(currentPos.Left + rightWidth, currentPos.Top);
                    }
                    break;

                case ConsoleKey.Home:
                    // Move to start of line
                    _buffer.MoveCursorToStart();
                    var homePos = Console.GetCursorPosition();
                    Console.SetCursorPosition(_promptDisplayWidth, homePos.Top);
                    break;

                case ConsoleKey.End:
                    // Move to end of line
                    _buffer.MoveCursorToEnd();
                    var endPos = Console.GetCursorPosition();
                    var totalWidth = _buffer.GetDisplayWidthAfterCursor();
                    Console.SetCursorPosition(endPos.Left + totalWidth, endPos.Top);
                    break;

                case ConsoleKey.Escape:
                    // Clear the entire line
                    _buffer.TryDrain(out _);
                    var clearPos = Console.GetCursorPosition();
                    Console.SetCursorPosition(_promptDisplayWidth, clearPos.Top);
                    Console.Write(new string(' ', Console.WindowWidth - _promptDisplayWidth - 1));
                    Console.SetCursorPosition(_promptDisplayWidth, clearPos.Top);
                    break;

                default:
                    // Handle Ctrl key combinations
                    if (key.Modifiers == ConsoleModifiers.Control)
                    {
                        switch (key.Key)
                        {
                            case ConsoleKey.U:
                                // Ctrl+U - Clear line from cursor to beginning
                                ClearLineFromCursorToStart();
                                break;

                            case ConsoleKey.A:
                                // Ctrl+A - Move to start of line (same as Home)
                                _buffer.MoveCursorToStart();
                                var ctrlAPos = Console.GetCursorPosition();
                                Console.SetCursorPosition(_promptDisplayWidth, ctrlAPos.Top);
                                break;

                            case ConsoleKey.E:
                                // Ctrl+E - Move to end of line (same as End)
                                _buffer.MoveCursorToEnd();
                                var ctrlEPos = Console.GetCursorPosition();
                                var ctrlETotalWidth = _buffer.GetDisplayWidthAfterCursor();
                                Console.SetCursorPosition(ctrlEPos.Left + ctrlETotalWidth, ctrlEPos.Top);
                                break;

                            case ConsoleKey.K:
                                // Ctrl+K - Clear line from cursor to end
                                ClearLineFromCursorToEnd();
                                break;

                            case ConsoleKey.W:
                                // Ctrl+W - Delete word before cursor
                                DeleteWordBeforeCursor();
                                break;

                            case ConsoleKey.L:
                                // Ctrl+L - Clear screen and redraw prompt
                                Console.Clear();
                                Console.SetCursorPosition(0, 0);
                                AnsiConsole.Markup(_prompt);
                                var text = _buffer.GetEntireText();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    Console.Write(text);
                                    // Reset cursor to its position within the text
                                    var cursorOffset = _buffer.GetDisplayWidthBeforeCursor();
                                    Console.SetCursorPosition(_promptDisplayWidth + cursorOffset, 0);
                                }
                                break;
                        }
                    }
                    // Regular character input
                    else if (key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
                    {
                        // Try to create a Rune from the character
                        if (System.Text.Rune.TryCreate(key.KeyChar, out var rune))
                        {
                            // Check if we're inserting in the middle
                            var isMiddleInsertion = _buffer.CursorPosition < _buffer.Length;

                            // Get the text after cursor BEFORE inserting
                            var afterTextBefore = isMiddleInsertion ? _buffer.GetTextAfterCursor() : string.Empty;

                            // Insert the character into the buffer
                            _buffer.Append(rune);

                            // Now handle display
                            if (isMiddleInsertion && !string.IsNullOrEmpty(afterTextBefore))
                            {
                                // We're inserting in the middle - need to redraw everything after insertion point
                                var currentPos = Console.GetCursorPosition();

                                // Write the new character
                                Console.Write(key.KeyChar);

                                // Write the text that was after the cursor
                                Console.Write(afterTextBefore);

                                // Clear any potential leftover characters
                                Console.Write(" ");

                                // Position cursor right after the newly inserted character
                                var charWidth = GetDisplayWidth(rune);
                                Console.SetCursorPosition(currentPos.Left + charWidth, currentPos.Top);
                            }
                            else
                            {
                                // Simple append at end
                                Console.Write(key.KeyChar);
                            }
                        }
                    }
                    break;
            }
        }
    }

    private static int GetDisplayWidth(System.Text.Rune rune)
    {
        // Wide character ranges for CJK and other wide characters
        var value = rune.Value;

        // CJK ranges
        if ((value >= 0x1100 && value <= 0x115F) ||
            (value >= 0x2329 && value <= 0x232A) ||
            (value >= 0x2E80 && value <= 0xA4CF) ||
            (value >= 0xAC00 && value <= 0xD7A3) ||
            (value >= 0xF900 && value <= 0xFAFF) ||
            (value >= 0xFE10 && value <= 0xFE6F) ||
            (value >= 0xFF00 && value <= 0xFF60) ||
            (value >= 0xFFE0 && value <= 0xFFE6))
        {
            return 2;
        }

        return 1;
    }

    private static int CalculateStringDisplayWidth(string text)
    {
        var width = 0;
        foreach (var c in text)
        {
            if (System.Text.Rune.TryCreate(c, out var rune))
            {
                width += GetDisplayWidth(rune);
            }
            else
            {
                // If it fails to create a rune, assume width of 1
                width += 1;
            }
        }
        return width;
    }

    private void ReplaceCurrentLine(string newText)
    {
        // Clear current line
        var currentPos = Console.GetCursorPosition();
        Console.SetCursorPosition(_promptDisplayWidth, currentPos.Top);
        var currentLength = _buffer.GetDisplayWidth();
        Console.Write(new string(' ', currentLength));
        Console.SetCursorPosition(_promptDisplayWidth, currentPos.Top);

        // Clear buffer and add new text
        _buffer.TryDrain(out _);
        foreach (var c in newText)
        {
            if (System.Text.Rune.TryCreate(c, out var rune))
            {
                _buffer.Append(rune);
            }
        }

        // Display new text
        Console.Write(newText);
    }

    private void ClearLineFromCursorToStart()
    {
        // Get text after cursor
        var afterText = _buffer.GetTextAfterCursor();

        // Clear buffer and add only the text after cursor
        _buffer.TryDrain(out _);
        foreach (var c in afterText)
        {
            if (System.Text.Rune.TryCreate(c, out var rune))
            {
                _buffer.Append(rune);
            }
        }
        _buffer.MoveCursorToStart();

        // Redraw the line
        var currentPos = Console.GetCursorPosition();
        Console.SetCursorPosition(_promptDisplayWidth, currentPos.Top);
        Console.Write(afterText);

        // Clear any leftover characters
        var spacesToClear = currentPos.Left - _promptDisplayWidth;
        if (spacesToClear > 0)
        {
            Console.Write(new string(' ', spacesToClear));
        }

        Console.SetCursorPosition(_promptDisplayWidth, currentPos.Top);
    }

    private void ClearLineFromCursorToEnd()
    {
        // Get text before cursor
        var beforeText = _buffer.GetTextBeforeCursor();

        // Get current cursor position for clearing
        var currentPos = Console.GetCursorPosition();

        // Calculate how much to clear
        var textAfterCursor = _buffer.GetTextAfterCursor();
        var widthToClear = 0;
        foreach (var c in textAfterCursor)
        {
            if (System.Text.Rune.TryCreate(c, out var rune))
            {
                widthToClear += GetDisplayWidth(rune);
            }
        }

        // Clear from cursor to end
        if (widthToClear > 0)
        {
            Console.Write(new string(' ', widthToClear));
            Console.SetCursorPosition(currentPos.Left, currentPos.Top);
        }

        // Update buffer
        _buffer.TryDrain(out _);
        foreach (var c in beforeText)
        {
            if (System.Text.Rune.TryCreate(c, out var rune))
            {
                _buffer.Append(rune);
            }
        }
    }

    private void DeleteWordBeforeCursor()
    {
        var text = _buffer.GetTextBeforeCursor();
        if (string.IsNullOrEmpty(text))
            return;

        // Find the last word boundary
        var lastIndex = text.Length - 1;

        // Skip trailing spaces
        while (lastIndex >= 0 && char.IsWhiteSpace(text[lastIndex]))
        {
            lastIndex--;
        }

        // Find start of word
        while (lastIndex >= 0 && !char.IsWhiteSpace(text[lastIndex]))
        {
            lastIndex--;
        }

        var newText = lastIndex >= 0 ? text.Substring(0, lastIndex + 1) : string.Empty;
        var afterText = _buffer.GetTextAfterCursor();

        // Calculate width to clear
        var deletedWidth = 0;
        for (int i = newText.Length; i < text.Length; i++)
        {
            if (System.Text.Rune.TryCreate(text[i], out var rune))
            {
                deletedWidth += GetDisplayWidth(rune);
            }
        }

        // Update buffer
        _buffer.TryDrain(out _);
        foreach (var c in newText + afterText)
        {
            if (System.Text.Rune.TryCreate(c, out var rune))
            {
                _buffer.Append(rune);
            }
        }

        // Move cursor to end of newText
        for (int i = 0; i < newText.Length; i++)
        {
            _buffer.MoveCursorLeft(out _);
        }

        // Redraw
        var currentPos = Console.GetCursorPosition();
        var newLeft = currentPos.Left - deletedWidth;
        Console.SetCursorPosition(newLeft, currentPos.Top);
        Console.Write(afterText);

        // Clear leftover characters
        if (deletedWidth > 0)
        {
            Console.Write(new string(' ', deletedWidth));
        }

        Console.SetCursorPosition(newLeft, currentPos.Top);
    }
}