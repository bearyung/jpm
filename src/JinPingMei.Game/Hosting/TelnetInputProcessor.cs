using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JinPingMei.Game.Hosting.Text;

namespace JinPingMei.Game.Hosting;

internal sealed class TelnetInputProcessor
{
    private const byte Backspace = 0x08;
    private const byte Delete = 0x7F;
    private const byte CarriageReturn = 0x0D;
    private const byte LineFeed = 0x0A;
    private const byte Iac = 0xFF;
    private const byte Do = 0xFD;
    private const byte Dont = 0xFE;
    private const byte Will = 0xFB;
    private const byte Wont = 0xFC;
    private const byte SubNegotiation = 0xFA;
    private const byte EndSubNegotiation = 0xF0;
    private const byte EchoOption = 0x01;
    private const byte SuppressGoAhead = 0x03;
    private const byte LineMode = 0x22;

    private readonly StreamWriter _writer;
    private readonly Stream _stream;
    private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
    private readonly GraphemeBuffer _buffer = new();

    private TelnetCommandState _telnetState = TelnetCommandState.Data;
    private byte _pendingCommand;
    private bool _pendingCarriageReturn;
    private readonly bool _echoInput;
    private AnsiEscapeState _ansiState = AnsiEscapeState.None;
    private readonly List<byte> _escapeBuffer = new();

    public TelnetInputProcessor(Stream stream, StreamWriter writer, bool echoInput = true)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _echoInput = echoInput;
    }

    public async Task InitializeNegotiationAsync(CancellationToken cancellationToken)
    {
        // Flush any pending output first
        await _writer.FlushAsync().ConfigureAwait(false);

        // Send all negotiation commands as a single batch to minimize visual artifacts
        var negotiationBytes = new byte[]
        {
            // IAC WILL ECHO
            Iac, Will, EchoOption,
            // IAC WILL SUPPRESS-GO-AHEAD
            Iac, Will, SuppressGoAhead,
            // IAC WONT LINEMODE
            Iac, Wont, LineMode,
            // IAC DO SUPPRESS-GO-AHEAD
            Iac, Do, SuppressGoAhead,
            // IAC DONT ECHO
            Iac, Dont, EchoOption
        };

        await _stream.WriteAsync(negotiationBytes, cancellationToken).ConfigureAwait(false);
        await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);

        // Small delay to let client process negotiation
        await Task.Delay(50, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var read = await _stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    return _buffer.TryDrain(out var partial) ? partial : null;
                }

                for (var i = 0; i < read; i++)
                {
                    var value = buffer[i];

                    if (_telnetState != TelnetCommandState.Data)
                    {
                        await HandleTelnetCommandByteAsync(value, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (value == Iac)
                    {
                        _telnetState = TelnetCommandState.Command;
                        continue;
                    }

                    // Check for ESC character (start of ANSI escape sequence)
                    if (value == 0x1B)
                    {
                        _ansiState = AnsiEscapeState.Escape;
                        _escapeBuffer.Clear();
                        continue;
                    }

                    // Handle ANSI escape sequence processing
                    if (_ansiState != AnsiEscapeState.None)
                    {
                        await HandleAnsiEscapeByteAsync(value).ConfigureAwait(false);
                        continue;
                    }

                    if (value == Backspace || value == Delete)
                    {
                        await HandleBackspaceAsync().ConfigureAwait(false);
                        continue;
                    }

                    if (value == CarriageReturn)
                    {
                        _pendingCarriageReturn = true;
                        return await CompleteLineAsync().ConfigureAwait(false);
                    }

                    if (value == LineFeed)
                    {
                        if (_pendingCarriageReturn)
                        {
                            _pendingCarriageReturn = false;
                            continue;
                        }

                        return await CompleteLineAsync().ConfigureAwait(false);
                    }

                    await DecodeAndAppendAsync(value, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async ValueTask DecodeAndAppendAsync(byte value, CancellationToken cancellationToken)
    {
        Span<byte> input = stackalloc byte[1] { value };
        Span<char> chars = stackalloc char[2];
        var count = _decoder.GetChars(input, chars, flush: false);
        if (count == 0)
        {
            return;
        }

        StringBuilder? echoBuilder = _echoInput ? new StringBuilder() : null;
        var runeBuffer = _echoInput ? new char[2] : Array.Empty<char>();
        var remaining = chars[..count];
        while (!remaining.IsEmpty)
        {
            var status = Rune.DecodeFromUtf16(remaining, out var rune, out var consumed);
            if (status != OperationStatus.Done)
            {
                break;
            }

            remaining = remaining[consumed..];

            // Check if we're inserting in the middle
            var isInsertingInMiddle = _buffer.CursorPosition < _buffer.Length;

            // Get text after cursor before insertion (if any)
            string? textAfterCursor = null;
            int widthAfterCursor = 0;
            if (isInsertingInMiddle && _echoInput)
            {
                textAfterCursor = _buffer.GetTextAfterCursor();
                widthAfterCursor = _buffer.GetDisplayWidthAfterCursor();
            }

            _buffer.Append(rune);

            if (echoBuilder is not null)
            {
                var written = rune.EncodeToUtf16(runeBuffer);
                echoBuilder.Append(runeBuffer.AsSpan(0, written));

                // If inserting in middle, we need to redraw the text after cursor
                if (isInsertingInMiddle && !string.IsNullOrEmpty(textAfterCursor))
                {
                    // Add the text that comes after
                    echoBuilder.Append(textAfterCursor);
                    // Add spaces to clear any leftover characters
                    echoBuilder.Append(' ', 2);
                    // Move cursor back to where it should be
                    if (widthAfterCursor > 0)
                    {
                        echoBuilder.Append(new string('\b', widthAfterCursor + 2));
                    }
                }
            }
        }

        if (echoBuilder is not null && echoBuilder.Length > 0)
        {
            await _writer.WriteAsync(echoBuilder.ToString()).ConfigureAwait(false);
        }
    }

    private async Task<string?> CompleteLineAsync()
    {
        if (_echoInput)
        {
            await _writer.WriteAsync("\r\n").ConfigureAwait(false);
        }

        _decoder.Reset();
        _pendingCarriageReturn = false;
        if (!_buffer.TryDrain(out var line))
        {
            return string.Empty;
        }

        return line;
    }

    private async Task HandleBackspaceAsync()
    {
        // Check if we're deleting in the middle
        var isMiddleDeletion = _buffer.CursorPosition < _buffer.Length;

        // Get text after cursor before deletion
        string? textAfterCursor = null;
        int widthAfterCursor = 0;
        if (isMiddleDeletion && _echoInput)
        {
            textAfterCursor = _buffer.GetTextAfterCursor();
            widthAfterCursor = _buffer.GetDisplayWidthAfterCursor();
        }

        if (!_buffer.TryBackspace(out var width))
        {
            return;
        }

        if (width <= 0)
        {
            width = 1;
        }

        if (!_echoInput)
        {
            return;
        }

        if (isMiddleDeletion && !string.IsNullOrEmpty(textAfterCursor))
        {
            // Move cursor back by the width of deleted character
            await _writer.WriteAsync(new string('\b', width)).ConfigureAwait(false);
            // Write the text that was after the cursor
            await _writer.WriteAsync(textAfterCursor).ConfigureAwait(false);
            // Write spaces to clear any leftover characters
            await _writer.WriteAsync(new string(' ', width)).ConfigureAwait(false);
            // Move cursor back to the correct position
            await _writer.WriteAsync(new string('\b', widthAfterCursor + width)).ConfigureAwait(false);
        }
        else
        {
            // Standard backspace at end of line
            var sequence = EraseSequences.ForWidth(width);
            await _writer.WriteAsync(sequence).ConfigureAwait(false);
        }
    }

    private async ValueTask HandleTelnetCommandByteAsync(byte value, CancellationToken cancellationToken)
    {
        switch (_telnetState)
        {
            case TelnetCommandState.Command:
                switch (value)
                {
                    case Iac:
                        await DecodeAndAppendAsync(value, cancellationToken).ConfigureAwait(false);
                        _telnetState = TelnetCommandState.Data;
                        break;
                    case Do:
                    case Dont:
                    case Will:
                    case Wont:
                        _pendingCommand = value;
                        _telnetState = TelnetCommandState.Option;
                        break;
                    case SubNegotiation:
                        _telnetState = TelnetCommandState.SubNegotiation;
                        break;
                    default:
                        _telnetState = TelnetCommandState.Data;
                        break;
                }
                break;
            case TelnetCommandState.Option:
                await RespondToOptionAsync(value, cancellationToken).ConfigureAwait(false);
                _telnetState = TelnetCommandState.Data;
                break;
            case TelnetCommandState.SubNegotiation:
                if (value == Iac)
                {
                    _telnetState = TelnetCommandState.SubNegotiationEnd;
                }
                break;
            case TelnetCommandState.SubNegotiationEnd:
                _telnetState = value == EndSubNegotiation
                    ? TelnetCommandState.Data
                    : TelnetCommandState.SubNegotiation;
                break;
        }
    }

    private async ValueTask RespondToOptionAsync(byte option, CancellationToken cancellationToken)
    {
        switch (_pendingCommand)
        {
            case Do:
                // Accept ECHO and SUPPRESS_GO_AHEAD requests
                if (option == EchoOption || option == SuppressGoAhead)
                {
                    await SendCommandAsync(Will, option, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await SendCommandAsync(Wont, option, cancellationToken).ConfigureAwait(false);
                }
                break;
            case Dont:
                await SendCommandAsync(Wont, option, cancellationToken).ConfigureAwait(false);
                break;
            case Will:
                // Accept client's SUPPRESS_GO_AHEAD offer
                if (option == SuppressGoAhead)
                {
                    await SendCommandAsync(Do, option, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await SendCommandAsync(Dont, option, cancellationToken).ConfigureAwait(false);
                }
                break;
            case Wont:
                break;
        }
    }

    private ValueTask SendCommandAsync(byte command, byte option, CancellationToken cancellationToken)
    {
        var payload = new byte[] { Iac, command, option };
        return _stream.WriteAsync(payload, cancellationToken);
    }

    private async Task HandleAnsiEscapeByteAsync(byte value)
    {
        switch (_ansiState)
        {
            case AnsiEscapeState.Escape:
                if (value == 0x5B) // '[' character
                {
                    _ansiState = AnsiEscapeState.Bracket;
                    _escapeBuffer.Add(value);
                }
                else
                {
                    // Not a recognized escape sequence, reset
                    _ansiState = AnsiEscapeState.None;
                    _escapeBuffer.Clear();
                }
                break;

            case AnsiEscapeState.Bracket:
            case AnsiEscapeState.Collecting:
                _escapeBuffer.Add(value);

                // Check if this completes a recognized sequence
                if (value >= 0x40 && value <= 0x7E) // Final byte range
                {
                    await ProcessAnsiSequenceAsync().ConfigureAwait(false);
                    _ansiState = AnsiEscapeState.None;
                    _escapeBuffer.Clear();
                }
                else if (_escapeBuffer.Count > 10) // Safety limit
                {
                    // Too long, probably not a valid sequence
                    _ansiState = AnsiEscapeState.None;
                    _escapeBuffer.Clear();
                }
                else
                {
                    _ansiState = AnsiEscapeState.Collecting;
                }
                break;
        }
    }

    private async Task ProcessAnsiSequenceAsync()
    {
        if (_escapeBuffer.Count == 0) return;

        var finalByte = _escapeBuffer[^1];

        // Handle arrow keys: ESC [ A/B/C/D
        if (_escapeBuffer.Count == 2 && _escapeBuffer[0] == 0x5B)
        {
            switch (finalByte)
            {
                case 0x41: // Up arrow - move to start of line
                    await HandleHomeKeyAsync().ConfigureAwait(false);
                    break;
                case 0x42: // Down arrow - move to end of line
                    await HandleEndKeyAsync().ConfigureAwait(false);
                    break;
                case 0x43: // Right arrow
                    await HandleRightArrowAsync().ConfigureAwait(false);
                    break;
                case 0x44: // Left arrow
                    await HandleLeftArrowAsync().ConfigureAwait(false);
                    break;
            }
        }
        // Handle Home/End: ESC [ H, ESC [ F, ESC [ 1 ~, ESC [ 4 ~
        else if (_escapeBuffer[0] == 0x5B)
        {
            if (finalByte == 0x48 || (_escapeBuffer.Count == 3 && _escapeBuffer[1] == 0x31 && finalByte == 0x7E))
            {
                await HandleHomeKeyAsync().ConfigureAwait(false);
            }
            else if (finalByte == 0x46 || (_escapeBuffer.Count == 3 && _escapeBuffer[1] == 0x34 && finalByte == 0x7E))
            {
                await HandleEndKeyAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task HandleLeftArrowAsync()
    {
        if (_buffer.MoveCursorLeft(out var width))
        {
            if (_echoInput && width > 0)
            {
                // Move cursor left by width positions
                var sequence = new string('\b', width);
                await _writer.WriteAsync(sequence).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleRightArrowAsync()
    {
        if (_buffer.MoveCursorRight(out var width))
        {
            if (_echoInput && width > 0)
            {
                // Move cursor right by width positions using ESC[C
                var sequence = $"\x1b[{width}C";
                await _writer.WriteAsync(sequence).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleHomeKeyAsync()
    {
        var totalWidth = 0;
        while (_buffer.MoveCursorLeft(out var width))
        {
            totalWidth += width;
        }

        if (_echoInput && totalWidth > 0)
        {
            // Move cursor to start
            var sequence = new string('\b', totalWidth);
            await _writer.WriteAsync(sequence).ConfigureAwait(false);
        }
    }

    private async Task HandleEndKeyAsync()
    {
        var totalWidth = 0;
        while (_buffer.MoveCursorRight(out var width))
        {
            totalWidth += width;
        }

        if (_echoInput && totalWidth > 0)
        {
            // Move cursor to end
            var sequence = $"\x1b[{totalWidth}C";
            await _writer.WriteAsync(sequence).ConfigureAwait(false);
        }
    }

    private enum TelnetCommandState
    {
        Data,
        Command,
        Option,
        SubNegotiation,
        SubNegotiationEnd
    }

    private enum AnsiEscapeState
    {
        None,
        Escape,
        Bracket,
        Collecting
    }

}
