# Testing Command History and Bash Shortcuts

## Features Implemented

### Command History Navigation
- **Up Arrow (↑)**: Navigate to previous command in history
- **Down Arrow (↓)**: Navigate to next command in history
- The current input is saved when you start navigating history

### Bash-style Shortcuts
- **Ctrl+A**: Move cursor to beginning of line
- **Ctrl+E**: Move cursor to end of line
- **Ctrl+U**: Clear from cursor to beginning of line
- **Ctrl+K**: Clear from cursor to end of line
- **Ctrl+W**: Delete word before cursor
- **Ctrl+L**: Clear screen and redraw prompt

### Existing Navigation
- **Left/Right Arrow**: Move cursor left/right
- **Home**: Move to start of line
- **End**: Move to end of line
- **Backspace**: Delete character before cursor
- **Delete**: Delete character at cursor
- **Escape**: Clear entire line

## Test Instructions

1. Run the game:
   ```bash
   dotnet run --project src/JinPingMei.Game
   ```

2. Test command history:
   - Enter several commands: `/help`, `/look`, `/commands`
   - Press Up Arrow to navigate through history
   - Press Down Arrow to return to newer commands

3. Test bash shortcuts:
   - Type a long command
   - Press Ctrl+A to jump to start
   - Press Ctrl+E to jump to end
   - Press Ctrl+U to clear from cursor to beginning
   - Press Ctrl+K to clear from cursor to end
   - Press Ctrl+W to delete the last word
   - Press Ctrl+L to clear screen

4. Test with Chinese characters:
   - Type: `/say 你好世界`
   - Use arrow keys to navigate within the text
   - Use shortcuts to edit the Chinese text

## Expected Behavior
- History should persist across commands within the session
- All shortcuts should work correctly with both ASCII and Chinese characters
- Cursor positioning should be accurate for wide characters