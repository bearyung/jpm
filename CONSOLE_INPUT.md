# Console UI Features

## Multibyte Character Support

The console game properly handles multibyte characters (Chinese, Japanese, Korean, Emoji, etc.) with correct backspace behavior.

### Implementation Details

- Uses `GraphemeBuffer` to handle Unicode grapheme clusters correctly
- Tracks display width for wide characters (CJK characters have width 2)
- Backspace properly removes entire grapheme clusters, not individual bytes
- Supports cursor movement (left/right arrows, home/end) with wide characters
- Handles emoji with skin tone modifiers as single units

### Testing Multibyte Input

Run the game interactively and test:
1. Type Chinese characters: 中文
2. Press backspace - deletes one complete character at a time
3. Use arrow keys to navigate through text
4. Mix ASCII and Chinese: Test測試

## Persistent Footer

The console game displays a persistent footer showing player stats and shortcuts (interactive mode only).

### Footer Features

- **Player Name**: Shows current player name or "旅人" (traveler) if not set
- **Location**: Current locale and scene (e.g., "清河城 › 清河集市")
- **Shortcuts**: Common keyboard shortcuts and commands
  - Ctrl+C: Exit game
  - /help: Show help
  - /quit: Quit game

### Footer Behavior

- Only displays in interactive terminals (not when input/output is redirected)
- Automatically truncates if terminal width is too small
- Updates dynamically when player moves to new locations
- Updates when player sets their name with `/name <名字>`

### Testing Footer

Run the game interactively:
```bash
dotnet run --project src/JinPingMei.Game
```

Footer should appear at bottom with:
```
────────────────────────────────────────────────────────────────
玩家: 旅人 | 位置: 清河城 › 清河集市 | Ctrl+C:退出 | /help:指令 | /quit:離線
```

Test with piped input (should not show footer):
```bash
echo "/quit" | dotnet run --project src/JinPingMei.Game
```

### Terminal Requirements

- UTF-8 encoding support (set automatically in Program.cs)
- Terminal must support wide character display and cursor positioning
- Works in most modern terminals (Terminal.app, iTerm2, Windows Terminal, etc.)
- Minimum terminal width recommended: 80 characters