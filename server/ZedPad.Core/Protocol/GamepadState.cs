namespace ZedPad.Core.Protocol;

/// <summary>One controller state snapshot (see docs/protocol.md, GamepadState).</summary>
public readonly record struct GamepadState(
    ushort Sequence,
    short LeftX,
    short LeftY,
    short RightX,
    short RightY,
    ushort Buttons,
    byte LeftTrigger,
    byte RightTrigger)
{
    /// <summary>True when everything except the sequence number matches.</summary>
    public bool SameInputAs(GamepadState other) =>
        this with { Sequence = 0 } == other with { Sequence = 0 };
}
