using System.Text;
using ZedPad.Server;
using ZedPad.Server.Networking;
using ZedPad.Server.Pairing;

Console.OutputEncoding = Encoding.UTF8;

var options = new ServerOptions();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{options.Port}");
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(logging =>
{
    logging.SingleLine = true;
    logging.TimestampFormat = "HH:mm:ss ";
});
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Services.AddSingleton(options);
builder.Services.AddControllerSocket();
builder.Services.AddSingleton<PairingPresenter>();
builder.Services.AddHostedService<PairingKeyListener>();

var app = builder.Build();
app.MapControllerSocket();
app.Lifetime.ApplicationStarted.Register(() =>
{
    ServerBanner.Print(options.Port);
    app.Services.GetRequiredService<PairingPresenter>().ShowNewCode();
});

await app.RunAsync();
