#!/bin/bash

# Connect to JinPingMei server with proper raw mode for arrow keys

echo "Connecting to JinPingMei server on port 2325..."
echo "Use Ctrl+C to disconnect"
echo ""

# Save terminal settings
SAVED_SETTINGS=$(stty -g)

# Function to restore terminal on exit
restore_terminal() {
    stty "$SAVED_SETTINGS"
    echo -e "\nConnection closed."
}

# Set up trap to restore terminal
trap restore_terminal EXIT

# Set terminal to raw mode
# -echo: don't echo typed characters (server will echo)
# -icanon: disable canonical mode (send chars immediately)
# -iexten: disable extended processing
stty -echo -icanon -iexten

# Connect with nc
nc 127.0.0.1 2325