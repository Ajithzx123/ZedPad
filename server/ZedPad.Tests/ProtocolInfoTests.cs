using ZedPad.Core.Protocol;

namespace ZedPad.Tests;

public class ProtocolInfoTests
{
    [Fact]
    public void GamepadStateFrameFitsMessageLimit()
    {
        Assert.True(ProtocolInfo.GamepadStateLength <= ProtocolInfo.MaxMessageSize);
    }
}
