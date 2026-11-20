using ZedPad.Core.Pairing;
using ZedPad.Server.Networking;

namespace ZedPad.Server.Pairing;

/// <summary>Shows a fresh pairing code (QR and typed code) on the console.</summary>
public sealed class PairingPresenter
{
    private readonly PairingTokenService _tokens;
    private readonly int _port;

    public PairingPresenter(PairingTokenService tokens, ServerOptions options)
    {
        _tokens = tokens;
        _port = options.Port;
    }

    public void ShowNewCode()
    {
        var code = _tokens.Issue();
        var hosts = LanAddresses.GetIPv4();
        var link = PairingLink.Create(hosts, _port, code.Token);

        // Written to the console on purpose: the user needs it to pair. It is never sent to the log.
        Console.WriteLine(QrConsoleRenderer.Render(link));
        Console.WriteLine();
        Console.WriteLine($"Pairing code: {PairingTokenService.Format(code.Token)}");
        Console.WriteLine($"Valid for {PairingTokenService.Lifetime.TotalMinutes:0} minutes, one use. Press R for a new code.");
        Console.WriteLine();
    }
}
