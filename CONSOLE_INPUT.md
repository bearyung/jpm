# Console Input Handling

## Multibyte Character Support

The console game now properly handles multibyte characters (Chinese, Japanese, Korean, Emoji, etc.) with correct backspace behavior.

### Implementation Details

- Uses `GraphemeBuffer` to handle Unicode grapheme clusters correctly
- Tracks display width for wide characters (CJK characters have width 2)
- Backspace properly removes entire grapheme clusters, not individual bytes
- Supports cursor movement (left/right arrows, home/end) with wide characters
- Handles emoji with skin tone modifiers as single units

### Testing

Run the game interactively and test:
1. Type Chinese characters: 中文
2. Press backspace - deletes one complete character at a time
3. Use arrow keys to navigate through text
4. Mix ASCII and Chinese: Test測試

The GraphemeBuffer implementation is thoroughly tested in `GraphemeBufferTests.cs`

### Terminal Requirements

- UTF-8 encoding support (set automatically in Program.cs)
- Terminal must support wide character display
- Works in most modern terminals (Terminal.app, iTerm2, Windows Terminal, etc.)