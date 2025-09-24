#!/usr/bin/env python3
"""
Clean connection script for JinPingMei telnet server.
Filters out telnet negotiation bytes for clean display.
"""

import socket
import sys
import select
import termios
import tty
import os

def main():
    print("Connecting to JinPingMei server on port 2325...")
    print("Use Ctrl+C to disconnect")
    print()

    # Save terminal settings
    old_settings = None
    if sys.stdin.isatty():
        old_settings = termios.tcgetattr(sys.stdin)

    sock = None
    try:
        # Connect to server
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.connect(('127.0.0.1', 2325))
        sock.setblocking(False)

        # Set terminal to raw mode if it's a TTY
        if old_settings:
            tty.setraw(sys.stdin.fileno())

        telnet_state = 'normal'
        telnet_count = 0

        while True:
            # Check for input/output
            readable, _, _ = select.select([sys.stdin, sock], [], [], 0.1)

            if sys.stdin in readable:
                # Read from stdin and send to socket
                data = os.read(sys.stdin.fileno(), 1024)
                if not data:
                    break
                # Check for Ctrl+C
                if b'\x03' in data:
                    break
                sock.send(data)

            if sock in readable:
                # Read from socket
                try:
                    data = sock.recv(4096)
                    if not data:
                        break

                    # Filter telnet negotiation bytes
                    filtered = bytearray()
                    i = 0
                    while i < len(data):
                        byte = data[i]

                        if telnet_state == 'normal':
                            if byte == 0xFF:  # IAC (Interpret As Command)
                                telnet_state = 'iac'
                            else:
                                filtered.append(byte)
                        elif telnet_state == 'iac':
                            if byte == 0xFF:  # Double IAC means literal 0xFF
                                filtered.append(0xFF)
                                telnet_state = 'normal'
                            elif byte in [0xFB, 0xFC, 0xFD, 0xFE]:  # WILL, WONT, DO, DONT
                                telnet_state = 'option'
                            else:
                                telnet_state = 'normal'
                        elif telnet_state == 'option':
                            # Skip the option byte
                            telnet_state = 'normal'

                        i += 1

                    # Write filtered output
                    if filtered:
                        sys.stdout.buffer.write(filtered)
                        sys.stdout.flush()

                except socket.error:
                    pass

    except KeyboardInterrupt:
        pass
    except Exception as e:
        print(f"\nError: {e}")
    finally:
        # Restore terminal settings
        if old_settings:
            termios.tcsetattr(sys.stdin, termios.TCSADRAIN, old_settings)
        if sock:
            sock.close()
        print("\nConnection closed.")

if __name__ == "__main__":
    main()