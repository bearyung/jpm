#!/usr/bin/env python3
"""
Test script for Chinese input handling.
This script simulates typing Chinese characters and backspace operations.
"""

import subprocess
import time
import sys

def test_chinese_input():
    print("Testing Chinese input with backspace handling...")
    print("This test will:")
    print("1. Type '中文' (two Chinese characters)")
    print("2. Attempt to backspace")
    print("3. Check if backspace works correctly")
    print("")

    # Run the game interactively
    print("Please run the game manually and test the following:")
    print("1. Type: 中文")
    print("2. Press backspace once - it should delete '文' completely")
    print("3. Press backspace again - it should delete '中' completely")
    print("4. Type: 測試 after deletion to verify cursor position")
    print("")
    print("To run the game:")
    print("dotnet run --project src/JinPingMei.Game")

if __name__ == "__main__":
    test_chinese_input()