namespace ZedPad.Server.Networking;

public static class ControllerSocketEndpoint
{
    public static void AddControllerSocket(this IServiceCollection services) =>
        services.AddSingleton<ControllerSocketHandler>();

    public static void MapControllerSocket(this WebApplication app)
    {
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
}
