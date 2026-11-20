using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using ZedPad.Core.Pairing;
using ZedPad.Server;
using ZedPad.Server.Networking;

namespace ZedPad.Tests;

public sealed class ControllerSocketTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private Uri _uri = null!;
    private PairingTokenService _tokens = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(new ServerOptions { PairingWindow = TimeSpan.FromMilliseconds(600) });
        builder.Services.AddControllerSocket();

        _app = builder.Build();
        _app.MapControllerSocket();
        await _app.StartAsync();

        _tokens = _app.Services.GetRequiredService<PairingTokenService>();
        var address = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _uri = new Uri(address.Replace("http://", "ws://", StringComparison.Ordinal));
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task ValidTokenPairsAndIsAcknowledged()
    {
        using var client = await ConnectAsync();

        var reply = await PairAsync(client, _tokens.Issue().Token);

        Assert.Contains("\"type\":\"pairAck\"", reply, StringComparison.Ordinal);
        Assert.Contains("\"slot\":1", reply, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TokenCannotBeReusedByASecondDevice()
    {
        var token = _tokens.Issue().Token;
        using var first = await ConnectAsync();
        await PairAsync(first, token);

        using var second = await ConnectAsync();
        var reply = await PairAsync(second, token);

        Assert.Contains("\"reason\":\"badToken\"", reply, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WrongTokenIsRejectedAndConnectionClosed()
    {
        _tokens.Issue();
        using var client = await ConnectAsync();

        var reply = await PairAsync(client, "AAAAAAAA");
        await ReceiveCloseAsync(client);

        Assert.Contains("\"reason\":\"badToken\"", reply, StringComparison.Ordinal);
        Assert.Equal(WebSocketCloseStatus.PolicyViolation, client.CloseStatus);
    }

    [Fact]
    public async Task UnsupportedVersionIsRejectedWithoutConsumingTheToken()
    {
        var token = _tokens.Issue().Token;
        using var wrongVersion = await ConnectAsync();

        var reject = await PairAsync(wrongVersion, token, version: 99);

        Assert.Contains("\"reason\":\"version\"", reject, StringComparison.Ordinal);

        using var correct = await ConnectAsync();
        var ack = await PairAsync(correct, token);
        Assert.Contains("pairAck", ack, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PingBeforePairingClosesConnection()
    {
        using var client = await ConnectAsync();

        await SendTextAsync(client, """{"type":"ping","t":1}""");
        await ReceiveCloseAsync(client);

        Assert.Equal(WebSocketCloseStatus.PolicyViolation, client.CloseStatus);
    }

    [Fact]
    public async Task StateFrameBeforePairingClosesConnection()
    {
        using var client = await ConnectAsync();

        await client.SendAsync(new byte[16], WebSocketMessageType.Binary, true, CancellationToken.None);
        await ReceiveCloseAsync(client);

        Assert.Equal(WebSocketCloseStatus.PolicyViolation, client.CloseStatus);
    }

    [Fact]
    public async Task ConnectionThatNeverPairsIsClosedAfterThePairingWindow()
    {
        using var client = await ConnectAsync();

        var buffer = new byte[16];
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var error = await Record.ExceptionAsync(() => client.ReceiveAsync(buffer, guard.Token));

        // The server aborts the socket when the pairing window runs out.
        Assert.IsType<WebSocketException>(error);
    }

    [Fact]
    public async Task PingIsAnsweredWithPongAfterPairing()
    {
        using var client = await ConnectAsync();
        await PairAsync(client, _tokens.Issue().Token);

        await SendTextAsync(client, """{"type":"ping","t":42}""");
        var reply = await ReceiveTextAsync(client);

        Assert.Equal("""{"type":"pong","t":42}""", reply);
    }

    [Fact]
    public async Task InvalidFrameIsDroppedWithoutClosingPairedConnection()
    {
        using var client = await ConnectAsync();
        await PairAsync(client, _tokens.Issue().Token);

        await client.SendAsync(new byte[5], WebSocketMessageType.Binary, true, CancellationToken.None);
        await SendTextAsync(client, """{"type":"ping","t":1}""");
        var reply = await ReceiveTextAsync(client);

        Assert.Equal("""{"type":"pong","t":1}""", reply);
    }

    [Fact]
    public async Task OversizedMessageClosesConnection()
    {
        using var client = await ConnectAsync();
        await PairAsync(client, _tokens.Issue().Token);

        await client.SendAsync(new byte[2048], WebSocketMessageType.Binary, true, CancellationToken.None);
        await ReceiveCloseAsync(client);

        Assert.Equal(WebSocketCloseStatus.MessageTooBig, client.CloseStatus);
    }

    private async Task<ClientWebSocket> ConnectAsync()
    {
        var client = new ClientWebSocket();
        await client.ConnectAsync(_uri, CancellationToken.None);
        return client;
    }

    private static async Task<string> PairAsync(ClientWebSocket client, string token, int version = 1)
    {
        await SendTextAsync(
            client,
            $$"""{"type":"pair","token":"{{token}}","deviceName":"test","version":{{version}}}""");
        return await ReceiveTextAsync(client);
    }

    private static Task SendTextAsync(ClientWebSocket client, string text) =>
        client.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);

    private static async Task<string> ReceiveTextAsync(ClientWebSocket client)
    {
        var buffer = new byte[256];
        var result = await client.ReceiveAsync(buffer, CancellationToken.None);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    private static async Task ReceiveCloseAsync(ClientWebSocket client)
    {
        var buffer = new byte[16];
        while (client.State == WebSocketState.Open)
        {
            await client.ReceiveAsync(buffer, CancellationToken.None);
        }
    }
}
