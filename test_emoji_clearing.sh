#!/bin/bash
# Test script for emoji clearing functionality

echo "Testing emoji clearing with Ctrl+K and Ctrl+W"
echo "================================================"
echo ""
echo "Test cases:"
echo "1. Type some emoji like 😀😀 and press Ctrl+K to clear to end"
echo "2. Type 'hello 😀😀' and press Ctrl+W to delete word"
echo "3. Type mixed text '你好 😀 world' and test various shortcuts"
echo ""
echo "Starting game..."
echo ""

dotnet run --project src/JinPingMei.Game