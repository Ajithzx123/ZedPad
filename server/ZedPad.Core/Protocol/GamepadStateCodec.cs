using System.Buffers.Binary;

namespace ZedPad.Core.Protocol;

public static class GamepadStateCodec
{
    private const ushort ReservedButtonBits = 0xC000;

    public static void Encode(in GamepadState state, Span<byte> destination)
    {
        if (destination.Length < ProtocolInfo.GamepadStateLength)
        {
            throw new ArgumentException("Destination is too small for a GamepadState frame.", nameof(destination));
        }

        destination[0] = ProtocolInfo.GamepadStateType;
        destination[1] = ProtocolInfo.Version;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[2..], state.Sequence);
        BinaryPrimitives.WriteInt16LittleEndian(destination[4..], state.LeftX);
        BinaryPrimitives.WriteInt16LittleEndian(destination[6..], state.LeftY);
        BinaryPrimitives.WriteInt16LittleEndian(destination[8..], state.RightX);
        BinaryPrimitives.WriteInt16LittleEndian(destination[10..], state.RightY);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[12..], state.Buttons);
        destination[14] = state.LeftTrigger;
        destination[15] = state.RightTrigger;
    }

    /// <summary>Decodes a frame; returns false for any frame the protocol says to drop.</summary>
    public static bool TryDecode(ReadOnlySpan<byte> frame, out GamepadState state)
    {
        state = default;

        if (frame.Length != ProtocolInfo.GamepadStateLength
            || frame[0] != ProtocolInfo.GamepadStateType
            || frame[1] != ProtocolInfo.Version)
        {
            return false;
        }

        var buttons = BinaryPrimitives.ReadUInt16LittleEndian(frame[12..]);
        if ((buttons & ReservedButtonBits) != 0)
        {
            return false;
        }

        state = new GamepadState(
            BinaryPrimitives.ReadUInt16LittleEndian(frame[2..]),
            BinaryPrimitives.ReadInt16LittleEndian(frame[4..]),
            BinaryPrimitives.ReadInt16LittleEndian(frame[6..]),
            BinaryPrimitives.ReadInt16LittleEndian(frame[8..]),
            BinaryPrimitives.ReadInt16LittleEndian(frame[10..]),
            buttons,
            frame[14],
            frame[15]);
        return true;
    }
}
