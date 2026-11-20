namespace ZedPad.Server;

public sealed record ServerOptions
{
    public int Port { get; init; } = 8765;

    /// <summary>How long a new connection has to complete pairing before it is closed.</summary>
    public TimeSpan PairingWindow { get; init; } = TimeSpan.FromSeconds(10);
}
