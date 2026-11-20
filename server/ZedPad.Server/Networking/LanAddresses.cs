using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ZedPad.Server.Networking;

public static class LanAddresses
{
    private static readonly string[] VirtualAdapterMarkers =
    [
        "vEthernet", "Hyper-V", "VirtualBox", "VMware", "WSL", "Loopback",
    ];

    /// <summary>IPv4 addresses a phone on the same network could reach, virtual adapters excluded.</summary>
    public static IReadOnlyList<string> GetIPv4() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                        && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                        && !IsVirtual(n))
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString())
            .ToList();

    private static bool IsVirtual(NetworkInterface adapter) =>
        VirtualAdapterMarkers.Any(marker =>
            adapter.Name.Contains(marker, StringComparison.OrdinalIgnoreCase)
            || adapter.Description.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
