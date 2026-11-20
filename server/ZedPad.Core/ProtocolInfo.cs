namespace ZedPad.Core.Protocol;

/// <summary>Constants from docs/protocol.md (v1).</summary>
public static class ProtocolInfo
{
    public const byte GamepadStateType = 0x30;
    public const byte Version = 1;
    public const int GamepadStateLength = 16;
    public const int MaxMessageSize = 1024;
}
