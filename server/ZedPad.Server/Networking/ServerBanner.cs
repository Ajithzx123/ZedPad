using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ZedPad.Server.Networking;

public static class ServerBanner
{
    public static void Print(int port)
    {
        Console.WriteLine("ZedPad Server");
        Console.WriteLine("Status: Listening");
        foreach (var address in LanAddresses())
        {
            Console.WriteLine($"Address: ws://{address}:{port}");
        }

        Console.WriteLine("Connected device: None");
        Console.WriteLine();
    }

    private static IEnumerable<string> LanAddresses() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                        && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString());
}
