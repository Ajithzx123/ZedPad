namespace ZedPad.Server.Networking;

public static class ServerBanner
{
    public static void Print(int port)
    {
        Console.WriteLine("ZedPad Server");
        Console.WriteLine("Status: Listening");
        foreach (var address in LanAddresses.GetIPv4())
        {
            Console.WriteLine($"Address: {address}:{port}");
        }

        Console.WriteLine();
    }
}
