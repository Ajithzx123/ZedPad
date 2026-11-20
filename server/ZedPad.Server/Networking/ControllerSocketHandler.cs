using System.Net.WebSockets;
using ZedPad.Core.Pairing;
using ZedPad.Core.Protocol;

namespace ZedPad.Server.Networking;

/// <summary>
/// One handler serves every connection. A connection must pair within the configured pairing
/// window; until then only a pair request is accepted. Input injection arrives in Phase 3.
/// </summary>
public sealed partial class ControllerSocketHandler
{
    // Slows down guessing; a failed attempt also costs the client its connection.
    private static readonly TimeSpan FailedPairDelay = TimeSpan.FromMilliseconds(500);

    private readonly ILogger<ControllerSocketHandler> _logger;
    private readonly PairingTokenService _tokens;
    private readonly ServerOptions _options;

    public ControllerSocketHandler(
        ILogger<ControllerSocketHandler> logger,
        PairingTokenService tokens,
        ServerOptions options)
    {
        _logger = logger;
        _tokens = tokens;
        _options = options;
    }

    private sealed class Connection(WebSocket socket, string remote)
    {
        public WebSocket Socket { get; } = socket;

        public string Remote { get; } = remote;

        public Guid? SessionId { get; set; }

        public GamepadState? LastState { get; set; }
    }

    public async Task HandleAsync(WebSocket socket, string remote, CancellationToken cancellationToken)
    {
        var connection = new Connection(socket, remote);
        LogConnected(remote);

        using var pairingTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        pairingTimeout.CancelAfter(_options.PairingWindow);

        try
        {
            await ReceiveLoopAsync(connection, pairingTimeout, cancellationToken);
        }
        catch (WebSocketException ex)
        {
            LogConnectionError(remote, ex.Message);
        }
        catch (OperationCanceledException) when (pairingTimeout.IsCancellationRequested
                                                 && !cancellationToken.IsCancellationRequested)
        {
            LogPairingTimeout(remote);
        }
        catch (OperationCanceledException)
        {
            // Server shutting down or client aborted; nothing to report.
        }

        LogDisconnected(remote);
    }

    private async Task ReceiveLoopAsync(
        Connection connection,
        CancellationTokenSource pairingTimeout,
        CancellationToken cancellationToken)
    {
        var socket = connection.Socket;
        var buffer = new byte[ProtocolInfo.MaxMessageSize];

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, pairingTimeout.Token);

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
            var wasPaired = connection.SessionId is not null;

            var keepOpen = result.MessageType == WebSocketMessageType.Binary
                ? HandleBinary(connection, message.Span)
                : await HandleTextAsync(connection, message, cancellationToken);

            if (!keepOpen)
            {
                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Not allowed", CancellationToken.None);
                return;
            }

            if (!wasPaired && connection.SessionId is not null)
            {
                pairingTimeout.CancelAfter(Timeout.InfiniteTimeSpan);
            }
        }
    }

    /// <returns>False when the connection must be closed.</returns>
    private bool HandleBinary(Connection connection, ReadOnlySpan<byte> frame)
    {
        if (connection.SessionId is null)
        {
            LogUnpairedAccess(connection.Remote);
            return false;
        }

        if (!GamepadStateCodec.TryDecode(frame, out var state))
        {
            LogDroppedFrame(connection.Remote, frame.Length);
            return true;
        }

        if (connection.LastState is { } previous && !SequenceNumber.IsNewer(state.Sequence, previous.Sequence))
        {
            return true;
        }

        if (connection.LastState is not { } before || !state.SameInputAs(before))
        {
            LogState(connection.Remote, state.Sequence, state.LeftX, state.LeftY, state.Buttons);
        }

        connection.LastState = state;
        return true;
    }

    /// <returns>False when the connection must be closed.</returns>
    private async Task<bool> HandleTextAsync(
        Connection connection,
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken)
    {
        var parsed = ControlMessages.Parse(message.Span);

        if (connection.SessionId is null)
        {
            if (parsed is PairRequest request)
            {
                return await HandlePairAsync(connection, request, cancellationToken);
            }

            LogUnpairedAccess(connection.Remote);
            return false;
        }

        switch (parsed)
        {
            case PingMessage ping:
                await connection.Socket.SendAsync(
                    ControlMessages.Pong(ping.Timestamp),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);
                break;
            default:
                LogUnknownMessage(connection.Remote);
                break;
        }

        return true;
    }

    private async Task<bool> HandlePairAsync(
        Connection connection,
        PairRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Version != ProtocolInfo.Version)
        {
            await RejectAsync(connection, PairRejectReason.Version, cancellationToken);
            return false;
        }

        var result = _tokens.TryConsume(request.Token);
        if (result != PairingResult.Success)
        {
            var reason = result == PairingResult.Expired ? PairRejectReason.Expired : PairRejectReason.BadToken;
            await Task.Delay(FailedPairDelay, cancellationToken);
            await RejectAsync(connection, reason, cancellationToken);
            return false;
        }

        connection.SessionId = Guid.NewGuid();
        await connection.Socket.SendAsync(
            ControlMessages.PairAck(connection.SessionId.Value, slot: 1),
            WebSocketMessageType.Text,
            true,
            cancellationToken);
        LogPaired(connection.Remote, request.DeviceName);
        return true;
    }

    private async Task RejectAsync(Connection connection, string reason, CancellationToken cancellationToken)
    {
        LogPairRejected(connection.Remote, reason);
        await connection.Socket.SendAsync(
            ControlMessages.PairReject(reason),
            WebSocketMessageType.Text,
            true,
            cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Device connected: {Remote}")]
    private partial void LogConnected(string remote);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device disconnected: {Remote}")]
    private partial void LogDisconnected(string remote);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device paired: {Remote} ({DeviceName})")]
    private partial void LogPaired(string remote, string deviceName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pairing rejected for {Remote}: {Reason}")]
    private partial void LogPairRejected(string remote, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Remote} did not pair in time; closing")]
    private partial void LogPairingTimeout(string remote);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Remote} sent data before pairing; closing")]
    private partial void LogUnpairedAccess(string remote);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection error from {Remote}: {Reason}")]
    private partial void LogConnectionError(string remote, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dropped invalid frame from {Remote} ({Length} bytes)")]
    private partial void LogDroppedFrame(string remote, int length);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignored unrecognized message from {Remote}")]
    private partial void LogUnknownMessage(string remote);

    [LoggerMessage(Level = LogLevel.Information, Message = "State from {Remote}: seq={Sequence} L=({LeftX},{LeftY}) buttons=0x{Buttons:X4}")]
    private partial void LogState(string remote, ushort sequence, short leftX, short leftY, ushort buttons);
}
