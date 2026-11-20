using ZedPad.Core.Protocol;

namespace ZedPad.Tests;

public class ProtocolInfoTests
{
    [Fact]
    public void GamepadStateFrameFitsMessageLimit()
    {
        Assert.Equal(16, ProtocolInfo.GamepadStateLength);
        Assert.True(ProtocolInfo.GamepadStateLength <= ProtocolInfo.MaxMessageSize);
    }
}
