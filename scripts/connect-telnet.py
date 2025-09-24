#!/usr/bin/env python3
"""
UTF-8 aware telnet client for JinPingMei server.
Properly handles telnet protocol while supporting CJK input.
"""

import socket
import sys
import select
import termios
import tty
import os
import struct

class TelnetClient:
    # Telnet commands
    IAC = 255  # Interpret As Command
    DONT = 254
    DO = 253
    WONT = 252
    WILL = 251

    # Telnet options
    ECHO = 1
    SUPPRESS_GO_AHEAD = 3
    LINEMODE = 34

    def __init__(self, host='127.0.0.1', port=2325):
        self.host = host
        self.port = port
        self.sock = None
        self.telnet_state = 'normal'
        self.telnet_command = None

    def send_telnet_command(self, command, option):
        """Send a telnet command"""
        self.sock.send(bytes([self.IAC, command, option]))

    def handle_telnet_command(self, command, option):
        """Handle incoming telnet negotiation"""
        if command == self.DO:
            if option in [self.ECHO, self.SUPPRESS_GO_AHEAD]:
                # Accept these options
                self.send_telnet_command(self.WILL, option)
            else:
                # Refuse other options
                self.send_telnet_command(self.WONT, option)
        elif command == self.WILL:
            if option == self.SUPPRESS_GO_AHEAD:
                # Accept suppress go-ahead
                self.send_telnet_command(self.DO, option)
            else:
                # Refuse other options
                self.send_telnet_command(self.DONT, option)
        elif command == self.DONT:
            self.send_telnet_command(self.WONT, option)
        elif command == self.WONT:
            pass  # Acknowledge

    def process_telnet_byte(self, byte):
        """Process telnet protocol bytes"""
        if self.telnet_state == 'normal':
            if byte == self.IAC:
                self.telnet_state = 'iac'
                return None
            return byte
        elif self.telnet_state == 'iac':
            if byte == self.IAC:
                # Double IAC means literal 255
                self.telnet_state = 'normal'
                return 255
            elif byte in [self.WILL, self.WONT, self.DO, self.DONT]:
                self.telnet_command = byte
                self.telnet_state = 'option'
                return None
            else:
                self.telnet_state = 'normal'
                return None
        elif self.telnet_state == 'option':
            # Handle the negotiation
            self.handle_telnet_command(self.telnet_command, byte)
            self.telnet_state = 'normal'
            return None

    def connect(self):
        """Connect to the telnet server"""
        print(f"Connecting to JinPingMei server at {self.host}:{self.port}...")
        print("Use Ctrl+C to disconnect")
        print()

        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.connect((self.host, self.port))
        self.sock.setblocking(False)

    def run(self):
        """Main client loop"""
        # Save terminal settings
        old_settings = None
        if sys.stdin.isatty():
            old_settings = termios.tcgetattr(sys.stdin)

        try:
            # Set terminal to raw mode
            if old_settings:
                tty.setraw(sys.stdin.fileno())

            while True:
                # Check for input/output
                readable, _, _ = select.select([sys.stdin, self.sock], [], [], 0.1)

                if sys.stdin in readable:
                    # Read UTF-8 input from terminal
                    data = os.read(sys.stdin.fileno(), 1024)
                    if not data:
                        break

                    # Check for Ctrl+C
                    if b'\x03' in data:
                        break

                    # Send to server
                    self.sock.send(data)

                if self.sock in readable:
                    # Read from socket
                    try:
                        data = self.sock.recv(4096)
                        if not data:
                            break

                        # Process telnet protocol and output
                        output = bytearray()
                        for byte in data:
                            processed = self.process_telnet_byte(byte)
                            if processed is not None:
                                output.append(processed)

                        if output:
                            sys.stdout.buffer.write(output)
                            sys.stdout.flush()

                    except socket.error:
                        pass

        except KeyboardInterrupt:
            pass
        finally:
            # Restore terminal settings
            if old_settings:
                termios.tcsetattr(sys.stdin, termios.TCSADRAIN, old_settings)
            if self.sock:
                self.sock.close()
            print("\nConnection closed.")

def main():
    client = TelnetClient()
    try:
        client.connect()
        client.run()
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    main()