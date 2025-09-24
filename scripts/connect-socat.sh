#!/bin/bash

# Connect to JinPingMei server using socat for proper raw terminal handling

echo "Connecting to JinPingMei server on port 2325..."
echo "Use Ctrl+C to disconnect"
echo ""

# Set UTF-8 locale
export LANG=zh_TW.UTF-8
export LC_ALL=zh_TW.UTF-8

# Use socat with raw terminal settings
# raw: put terminal in raw mode (character-at-a-time, no local echo)
# echo=0: disable local echo
# icanon=0: disable canonical mode
socat -,raw,echo=0,icanon=0 TCP:127.0.0.1:2325