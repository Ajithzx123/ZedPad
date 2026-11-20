namespace ZedPad.Core.Pairing;

/// <summary>The URI encoded in the pairing QR code (see docs/protocol.md).</summary>
public static class PairingLink
{
    public static string Create(IEnumerable<string> hosts, int port, string token)
    {
        var parts = hosts.Select(h => $"h={Uri.EscapeDataString(h)}")
            .Append($"p={port}")
            .Append($"t={Uri.EscapeDataString(token)}");
        return $"zedpad://pair?{string.Join('&', parts)}";
    }
}
