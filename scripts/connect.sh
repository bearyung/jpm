#!/bin/bash

# Connect to JinPingMei server with proper UTF-8 support

echo "Connecting to JinPingMei server on port 2325..."
echo "Use Ctrl+C to disconnect"
echo ""

# Set UTF-8 locale
export LANG=zh_TW.UTF-8
export LC_ALL=zh_TW.UTF-8

# Use rlwrap with nc for better line editing and UTF-8 support
if command -v rlwrap &> /dev/null; then
    rlwrap -a -c nc 127.0.0.1 2325
else
    nc 127.0.0.1 2325
fi