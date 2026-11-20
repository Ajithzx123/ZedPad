namespace ZedPad.Server.Networking;

public static partial class ControllerSocketEndpoint
{
    public static void AddControllerSocket(this IServiceCollection services) =>
        services.AddSingleton<ControllerSocketHandler>();

    public static void MapControllerSocket(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            LogIncomingRequest(
                app.Logger,
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.Request.Headers.Upgrade.ToString());
            await next();
        });

        app.UseWebSockets();
        app.Map("/", async (HttpContext context, ControllerSocketHandler handler) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            var remote = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await handler.HandleAsync(socket, remote, context.RequestAborted);
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Incoming {Method} {Path} from {Remote} (upgrade: '{Upgrade}')")]
    private static partial void LogIncomingRequest(ILogger logger, string method, string path, string remote, string upgrade);
}
