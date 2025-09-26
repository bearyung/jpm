#!/bin/bash

# Test script for Chinese character input and cursor navigation
echo "測試中文輸入與游標控制 - Testing Chinese Input & Cursor Navigation"
echo "================================================================"
echo ""
echo "This script will test Chinese character input and cursor movement."
echo ""
echo "Test Instructions:"
echo "------------------"
echo "1. Type mixed text: Hello 中文測試 World"
echo "2. Use ← → arrow keys to move cursor left/right"
echo "3. Use Home/End keys to jump to start/end of line"
echo "4. Use Backspace to delete characters before cursor"
echo "5. Use Delete key to delete characters at cursor"
echo "6. Use Escape key to clear the entire line"
echo ""
echo "Expected behavior:"
echo "- Each arrow key press should move by one character (including Chinese)"
echo "- Backspace should delete exactly one character (including Chinese)"
echo "- Cursor should move correctly through mixed ASCII and Chinese text"
echo ""
echo "Starting game..."
echo ""

dotnet run --project src/JinPingMei.Game