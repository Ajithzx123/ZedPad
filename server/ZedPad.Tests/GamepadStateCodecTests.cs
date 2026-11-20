using System.Text.Json;
using ZedPad.Core.Protocol;

namespace ZedPad.Tests;

public class GamepadStateCodecTests
{
    private sealed record Vector(
        string Name,
        ushort Sequence,
        short LeftX,
        short LeftY,
        short RightX,
        short RightY,
        ushort Buttons,
        byte LeftTrigger,
        byte RightTrigger,
        string Hex)
    {
        public GamepadState State => new(Sequence, LeftX, LeftY, RightX, RightY, Buttons, LeftTrigger, RightTrigger);

        public byte[] Bytes => Convert.FromHexString(Hex.Replace(" ", string.Empty, StringComparison.Ordinal));

        public override string ToString() => Name;
    }

    public static TheoryData<string> VectorNames()
    {
        var data = new TheoryData<string>();
        foreach (var vector in LoadVectors())
        {
            data.Add(vector.Name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(VectorNames))]
    public void EncodeMatchesSharedVector(string name)
    {
        var vector = LoadVectors().Single(v => v.Name == name);
        var buffer = new byte[ProtocolInfo.GamepadStateLength];

        GamepadStateCodec.Encode(vector.State, buffer);

        Assert.Equal(vector.Bytes, buffer);
    }

    [Theory]
    [MemberData(nameof(VectorNames))]
    public void DecodeMatchesSharedVector(string name)
    {
        var vector = LoadVectors().Single(v => v.Name == name);

        var ok = GamepadStateCodec.TryDecode(vector.Bytes, out var state);

        Assert.True(ok);
        Assert.Equal(vector.State, state);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public void DecodeRejectsWrongLength(int length)
    {
        Assert.False(GamepadStateCodec.TryDecode(new byte[length], out _));
    }

    [Fact]
    public void DecodeRejectsWrongTypeVersionAndReservedBits()
    {
        var good = LoadVectors().First().Bytes;

        var wrongType = (byte[])good.Clone();
        wrongType[0] = 0x31;
        var wrongVersion = (byte[])good.Clone();
        wrongVersion[1] = 2;
        var reserved = (byte[])good.Clone();
        reserved[13] = 0x80;

        Assert.False(GamepadStateCodec.TryDecode(wrongType, out _));
        Assert.False(GamepadStateCodec.TryDecode(wrongVersion, out _));
        Assert.False(GamepadStateCodec.TryDecode(reserved, out _));
    }

    [Fact]
    public void EncodeThrowsWhenDestinationTooSmall()
    {
        var state = default(GamepadState);

        Assert.Throws<ArgumentException>(() => GamepadStateCodec.Encode(state, new byte[15]));
    }

    [Theory]
    [InlineData(1, 0, true)]
    [InlineData(0, 0, false)]
    [InlineData(0, 1, false)]
    [InlineData(0, 65535, true)]
    [InlineData(32768, 0, false)]
    public void SequenceNumberHandlesWraparound(ushort candidate, ushort last, bool expected)
    {
        Assert.Equal(expected, SequenceNumber.IsNewer(candidate, last));
    }

    private static List<Vector> LoadVectors()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "protocol-vectors.json");
        return JsonSerializer.Deserialize<List<Vector>>(File.ReadAllText(path), JsonOptions)!;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
}
