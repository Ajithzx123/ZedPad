using ZedPad.Server.Networking;

const int Port = 8765;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{Port}");
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});
builder.Services.AddControllerSocket();

var app = builder.Build();
app.MapControllerSocket();
app.Lifetime.ApplicationStarted.Register(() => ServerBanner.Print(Port));

await app.RunAsync();
