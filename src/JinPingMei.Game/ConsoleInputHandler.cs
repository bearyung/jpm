using System;
using System.Text;
using JinPingMei.Game.Hosting.Text;
using Spectre.Console;

namespace JinPingMei.Game;

/// <summary>
/// Custom console input handler that supports cursor movement and proper Unicode handling
/// </summary>
internal sealed class ConsoleInputHandler
{
    private readonly GraphemeBuffer _buffer = new();
    private readonly string _prompt;
    private readonly int _promptDisplayWidth;

    public ConsoleInputHandler(string prompt)
    {
        _prompt = prompt;
        // Calculate the actual display width of the prompt (without markup)
        var plainPrompt = prompt.RemoveMarkup();
        _promptDisplayWidth = plainPrompt.Length;
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
                    // Regular character input
                    if (key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
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
}