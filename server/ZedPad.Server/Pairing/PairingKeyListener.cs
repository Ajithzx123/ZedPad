namespace ZedPad.Server.Pairing;

/// <summary>Lets the operator press R on the console to show a new pairing code.</summary>
public sealed class PairingKeyListener(PairingPresenter presenter) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Console.IsInputRedirected)
        {
            return Task.CompletedTask;
        }

        return Task.Run(
            async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (Console.KeyAvailable
                        && Console.ReadKey(intercept: true).Key == ConsoleKey.R)
                    {
                        presenter.ShowNewCode();
                    }

                    await Task.Delay(100, stoppingToken).ConfigureAwait(false);
                }
            },
            stoppingToken);
    }
}
