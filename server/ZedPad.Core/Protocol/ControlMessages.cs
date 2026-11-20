using System.Text.Json;

namespace ZedPad.Core.Protocol;

public abstract record ControlMessage;

public sealed record PairRequest(string Token, string DeviceName, int Version) : ControlMessage;

public sealed record PingMessage(long Timestamp) : ControlMessage;

/// <summary>A well-formed JSON message of a type this version does not understand.</summary>
public sealed record UnknownMessage : ControlMessage;

public static class PairRejectReason
{
    public const string BadToken = "badToken";
    public const string Expired = "expired";
    public const string Version = "version";
    public const string Busy = "busy";
}

/// <summary>Text (JSON) control messages from docs/protocol.md.</summary>
public static class ControlMessages
{
    public static byte[] PairAck(Guid sessionId, int slot) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            type = "pairAck",
            sessionId,
            slot,
            version = ProtocolInfo.Version,
        });

    public static byte[] PairReject(string reason) =>
        JsonSerializer.SerializeToUtf8Bytes(new { type = "pairReject", reason });

    public static byte[] Pong(long timestamp) =>
        JsonSerializer.SerializeToUtf8Bytes(new { type = "pong", t = timestamp });

    /// <summary>Parses an incoming message; returns null if it is not a JSON object with a type.</summary>
    public static ControlMessage? Parse(ReadOnlySpan<byte> utf8)
    {
        try
        {
            var reader = new Utf8JsonReader(utf8);
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var type))
            {
                return null;
            }

            return type.GetString() switch
            {
                "pair" => new PairRequest(
                    GetString(root, "token"),
                    GetString(root, "deviceName"),
                    root.TryGetProperty("version", out var v) && v.TryGetInt32(out var version) ? version : 0),
                "ping" => new PingMessage(
                    root.TryGetProperty("t", out var t) && t.TryGetInt64(out var timestamp) ? timestamp : 0),
                _ => new UnknownMessage(),
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
