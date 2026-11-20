using System.Net.WebSockets;
using System.Text.Json;
using ZedPad.Core.Protocol;

namespace ZedPad.Server.Networking;

/// <summary>
/// Phase 1 spike: accepts any connection, answers pings and logs controller state.
/// Pairing and input injection arrive in later phases.
/// </summary>
public sealed partial class ControllerSocketHandler
{
    private readonly ILogger<ControllerSocketHandler> _logger;

    public ControllerSocketHandler(ILogger<ControllerSocketHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(WebSocket socket, string remote, CancellationToken cancellationToken)
    {
        LogConnected(remote);
        try
        {
            await ReceiveLoopAsync(socket, remote, cancellationToken);
        }
        catch (WebSocketException ex)
        {
            LogConnectionError(remote, ex.Message);
        }
        catch (OperationCanceledException)
        {
            // Server shutting down or client aborted; nothing to report.
        }

        LogDisconnected(remote);
    }

    private async Task ReceiveLoopAsync(WebSocket socket, string remote, CancellationToken cancellationToken)
    {
        var buffer = new byte[ProtocolInfo.MaxMessageSize];
        GamepadState? last = null;

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                return;
            }

            if (!result.EndOfMessage)
            {
                await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message too large", CancellationToken.None);
                return;
            }

            var message = buffer.AsMemory(0, result.Count);
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                last = HandleState(message.Span, last, remote);
            }
            else
            {
                await HandleTextAsync(socket, message, remote, cancellationToken);
            }
        }
    }

    private GamepadState? HandleState(ReadOnlySpan<byte> frame, GamepadState? last, string remote)
    {
        if (!GamepadStateCodec.TryDecode(frame, out var state))
        {
            LogDroppedFrame(remote, frame.Length);
            return last;
        }

        if (last is { } previous && !SequenceNumber.IsNewer(state.Sequence, previous.Sequence))
        {
            return last;
        }

        if (last is not { } before || !state.SameInputAs(before))
        {
            LogState(remote, state.Sequence, state.LeftX, state.LeftY, state.Buttons);
        }

        return state;
    }

    private async Task HandleTextAsync(WebSocket socket, ReadOnlyMemory<byte> message, string remote, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;
            if (root.TryGetProperty("type", out var type) && type.GetString() == "ping")
            {
                var timestamp = root.TryGetProperty("t", out var t) && t.TryGetInt64(out var value) ? value : 0;
                var pong = JsonSerializer.SerializeToUtf8Bytes(new { type = "pong", t = timestamp });
                await socket.SendAsync(pong, WebSocketMessageType.Text, true, cancellationToken);
                return;
            }

            LogUnknownMessage(remote);
        }
        catch (JsonException)
        {
            LogUnknownMessage(remote);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Device connected: {Remote}")]
    private partial void LogConnected(string remote);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device disconnected: {Remote}")]
    private partial void LogDisconnected(string remote);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection error from {Remote}: {Reason}")]
    private partial void LogConnectionError(string remote, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dropped invalid frame from {Remote} ({Length} bytes)")]
    private partial void LogDroppedFrame(string remote, int length);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignored unrecognized text message from {Remote}")]
    private partial void LogUnknownMessage(string remote);

    [LoggerMessage(Level = LogLevel.Information, Message = "State from {Remote}: seq={Sequence} L=({LeftX},{LeftY}) buttons=0x{Buttons:X4}")]
    private partial void LogState(string remote, ushort sequence, short leftX, short leftY, ushort buttons);
}
