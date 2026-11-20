using System.Text;
using ZedPad.Core.Protocol;

namespace ZedPad.Tests;

public class ControlMessagesTests
{
    private static ControlMessage? Parse(string json) => ControlMessages.Parse(Encoding.UTF8.GetBytes(json));

    [Fact]
    public void ParsesPairRequest()
    {
        var message = Parse("""{"type":"pair","token":"ABCD2345","deviceName":"Pixel","version":1}""");

        Assert.Equal(new PairRequest("ABCD2345", "Pixel", 1), message);
    }

    [Fact]
    public void PairRequestMissingFieldsGetsDefaults()
    {
        var message = Parse("""{"type":"pair"}""");

        Assert.Equal(new PairRequest(string.Empty, string.Empty, 0), message);
    }

    [Fact]
    public void ParsesPing()
    {
        Assert.Equal(new PingMessage(42), Parse("""{"type":"ping","t":42}"""));
    }

    [Fact]
    public void UnknownTypeIsReportedAsUnknown()
    {
        Assert.IsType<UnknownMessage>(Parse("""{"type":"dance"}"""));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("""{"noType":true}""")]
    [InlineData("")]
    public void NonMessagesReturnNull(string json)
    {
        Assert.Null(Parse(json));
    }

    [Fact]
    public void OutgoingMessagesUseTheDocumentedShape()
    {
        Assert.Equal("""{"type":"pong","t":7}""", Encoding.UTF8.GetString(ControlMessages.Pong(7)));
        Assert.Equal(
            """{"type":"pairReject","reason":"badToken"}""",
            Encoding.UTF8.GetString(ControlMessages.PairReject(PairRejectReason.BadToken)));

        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Assert.Equal(
            """{"type":"pairAck","sessionId":"11111111-2222-3333-4444-555555555555","slot":1,"version":1}""",
            Encoding.UTF8.GetString(ControlMessages.PairAck(id, 1)));
    }
}
