using Net.Codecrete.QrCodeGenerator;

namespace ZedPad.Server.Pairing;

public static class QrConsoleRenderer
{
    private const int QuietZone = 2;

    /// <summary>
    /// Renders the QR code with half-block characters, two modules per text row. Light modules are
    /// drawn as filled blocks and dark modules as empty cells, which is the right polarity on a
    /// dark console background.
    /// </summary>
    public static string Render(string text)
    {
        var qr = QrCode.EncodeText(text, QrCode.Ecc.Medium);
        var size = qr.Size + (2 * QuietZone);
        var lines = new List<string>();

        for (var y = 0; y < size; y += 2)
        {
            var line = new char[size];
            for (var x = 0; x < size; x++)
            {
                var topLight = IsLight(qr, x, y);
                var bottomLight = IsLight(qr, x, y + 1);
                line[x] = (topLight, bottomLight) switch
                {
                    (true, true) => '█',
                    (true, false) => '▀',
                    (false, true) => '▄',
                    _ => ' ',
                };
            }

            lines.Add(new string(line));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool IsLight(QrCode qr, int x, int y)
    {
        var mx = x - QuietZone;
        var my = y - QuietZone;
        return mx < 0 || my < 0 || mx >= qr.Size || my >= qr.Size || !qr.GetModule(mx, my);
    }
}
