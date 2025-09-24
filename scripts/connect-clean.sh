#!/bin/bash

# Connect to JinPingMei server with clean output (no telnet negotiation bytes)
# Uses socat if available, otherwise uses a Python wrapper

echo "Connecting to JinPingMei server on port 2325..."
echo "Use Ctrl+C to disconnect"
echo ""

# Check if socat is available
if command -v socat &> /dev/null; then
    # Use socat for clean connection
    socat -,raw,echo=0,escape=0x03 tcp:127.0.0.1:2325
elif command -v python3 &> /dev/null; then
    # Use Python to handle the connection and filter telnet bytes
    python3 - <<'EOF'
import socket
import sys
import select
import termios
import tty

def connect_telnet():
    # Save terminal settings
    old_settings = termios.tcgetattr(sys.stdin)

    try:
        # Set terminal to raw mode
        tty.setraw(sys.stdin.fileno())

        # Connect to server
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.connect(('127.0.0.1', 2325))
        sock.setblocking(False)

        # Skip initial telnet negotiation bytes
        import time
        time.sleep(0.1)
        try:
            initial = sock.recv(1024, socket.MSG_DONTWAIT)
            # Filter out telnet negotiation and print the rest
            filtered = bytearray()
            i = 0
            while i < len(initial):
                if initial[i] == 0xFF and i + 2 < len(initial):
                    # Skip IAC command sequence
                    i += 3
                else:
                    filtered.append(initial[i])
                    i += 1
            sys.stdout.buffer.write(filtered)
            sys.stdout.flush()
        except:
            pass

        # Main loop
        while True:
            # Check for input/output
            readable, _, _ = select.select([sys.stdin, sock], [], [], 0.1)

            if sys.stdin in readable:
                # Read from stdin and send to socket
                data = sys.stdin.read(1)
                if data:
                    sock.send(data.encode('utf-8'))
                    if ord(data) == 3:  # Ctrl+C
                        break

            if sock in readable:
                # Read from socket and write to stdout
                try:
                    data = sock.recv(1024)
                    if not data:
                        break
                    sys.stdout.buffer.write(data)
                    sys.stdout.flush()
                except:
                    pass

    except KeyboardInterrupt:
        pass
    finally:
        # Restore terminal settings
        termios.tcsetattr(sys.stdin, termios.TCSADRAIN, old_settings)
        sock.close()
        print("\nConnection closed.")

if __name__ == "__main__":
    connect_telnet()
EOF
else
    # Fallback to basic nc with warning
    echo "Warning: socat and python3 not found, using basic nc (may show telnet bytes)"
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
    stty -echo -icanon -iexten

    # Connect with nc
    nc 127.0.0.1 2325
fi