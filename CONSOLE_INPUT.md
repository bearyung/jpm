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

## Status Footer

The console game displays status information and shortcuts after each interaction, like traditional CLI tools (git, npm, etc.).

### Footer Features

- **Player Name**: Shows current player name or "旅人" (traveler) if not set
- **Location**: Current locale and scene (e.g., "清河城 › 清河集市")
- **Shortcuts**: Common keyboard shortcuts and commands
  - Ctrl+C: Exit game
  - /help: Show help
  - /quit: Quit game

### Footer Behavior

- **Natural Flow**: Appears inline after each command response, preserving scroll history
- **Scrollback Support**: All interactions remain in terminal history for scrolling back
- **CLI-Style**: Behaves like `git status`, `npm run`, and other CLI tools
- **Interactive Separator**: Shows visual separator (`────`) in interactive terminals
- **Clean Piped Output**: Minimal footer for automated/scripted usage
- **Auto-truncate**: Automatically truncates for narrow terminals in interactive mode

### Testing Footer

Run the game interactively and see natural flow:
```bash
dotnet run --project src/JinPingMei.Game
# Try multiple commands: /help, /look, etc.
# Scroll back in terminal to see full history
```

Example output flow:
```
[previous content remains visible]
────────────────────────────────────────────────
玩家: 旅人 | 位置: 清河城 › 清河集市 | Ctrl+C:退出 | /help:指令 | /quit:離線

> /help
目前可用指令：/help、/name <名字>、/look...

────────────────────────────────────────────────
玩家: 旅人 | 位置: 清河城 › 清河集市 | Ctrl+C:退出 | /help:指令 | /quit:離線

> /look
您身處於：清河集市...
```

Test clean piped output:
```bash
echo -e "/help\n/quit" | dotnet run --project src/JinPingMei.Game
```

### Terminal Requirements

- UTF-8 encoding support (set automatically in Program.cs)
- Terminal must support wide character display and cursor positioning
- Works in most modern terminals (Terminal.app, iTerm2, Windows Terminal, etc.)
- Minimum terminal width recommended: 80 characters