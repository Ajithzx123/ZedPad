namespace ZedPad.Core.Protocol;

public static class SequenceNumber
{
    /// <summary>Wraparound-safe check that <paramref name="candidate"/> is newer than <paramref name="last"/>.</summary>
    public static bool IsNewer(ushort candidate, ushort last) =>
        (short)(candidate - last) > 0;
}
