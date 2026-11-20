using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using ZedPad.Server.Networking;

namespace ZedPad.Tests;

public sealed class ControllerSocketTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private Uri _uri = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllerSocket();

        _app = builder.Build();
        _app.MapControllerSocket();
        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _uri = new Uri(address.Replace("http://", "ws://", StringComparison.Ordinal));
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task PingIsAnsweredWithPong()
    {
        using var client = await ConnectAsync();

        await SendTextAsync(client, """{"type":"ping","t":42}""");
        var reply = await ReceiveTextAsync(client);

        Assert.Equal("""{"type":"pong","t":42}""", reply);
    }

    [Fact]
    public async Task InvalidFrameIsDroppedWithoutClosingConnection()
    {
        using var client = await ConnectAsync();

        await client.SendAsync(new byte[5], WebSocketMessageType.Binary, true, CancellationToken.None);
        await SendTextAsync(client, """{"type":"ping","t":1}""");
        var reply = await ReceiveTextAsync(client);

        Assert.Equal("""{"type":"pong","t":1}""", reply);
    }

    [Fact]
    public async Task OversizedMessageClosesConnection()
    {
        using var client = await ConnectAsync();

        await client.SendAsync(new byte[2048], WebSocketMessageType.Binary, true, CancellationToken.None);
        var buffer = new byte[16];
        await client.ReceiveAsync(buffer, CancellationToken.None);

        Assert.Equal(WebSocketCloseStatus.MessageTooBig, client.CloseStatus);
    }

    private async Task<ClientWebSocket> ConnectAsync()
    {
        var client = new ClientWebSocket();
        await client.ConnectAsync(_uri, CancellationToken.None);
        return client;
    }

    private static Task SendTextAsync(ClientWebSocket client, string text) =>
        client.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);

    private static async Task<string> ReceiveTextAsync(ClientWebSocket client)
    {
        var buffer = new byte[256];
        var result = await client.ReceiveAsync(buffer, CancellationToken.None);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }
}
