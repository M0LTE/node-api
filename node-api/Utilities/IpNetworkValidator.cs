using System.Net;
using System.Net.Sockets;

namespace node_api.Utilities;

/// <summary>
/// Utility class for validating IP addresses against CIDR network ranges
/// </summary>
public static class IpNetworkValidator
{
    /// <summary>
    /// Checks if an IP address is within any of the specified CIDR network ranges
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <param name="allowedNetworks">List of CIDR notation networks (e.g., "192.168.1.0/24", "0.0.0.0/0")</param>
    /// <returns>True if the IP is within an allowed network, false otherwise</returns>
    public static bool IsIpAllowed(IPAddress ipAddress, IEnumerable<string> allowedNetworks)
    {
        if (ipAddress == null)
            return false;

        foreach (var network in allowedNetworks)
        {
            if (IsInNetwork(ipAddress, network))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if an IP address is within a specific CIDR network range
    /// </summary>
    private static bool IsInNetwork(IPAddress ipAddress, string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                return false;

            var networkAddress = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);

            // Convert IP addresses to bytes for comparison
            var ipBytes = ipAddress.GetAddressBytes();
            var networkBytes = networkAddress.GetAddressBytes();

            // IPv4 and IPv6 must match
            if (ipBytes.Length != networkBytes.Length)
                return false;

            // Calculate the number of bits to compare
            var bytesToCompare = prefixLength / 8;
            var bitsToCompare = prefixLength % 8;

            // Compare full bytes
            for (int i = 0; i < bytesToCompare; i++)
            {
                if (ipBytes[i] != networkBytes[i])
                    return false;
            }

            // Compare remaining bits if any
            if (bitsToCompare > 0 && bytesToCompare < ipBytes.Length)
            {
                var mask = (byte)(0xFF << (8 - bitsToCompare));
                if ((ipBytes[bytesToCompare] & mask) != (networkBytes[bytesToCompare] & mask))
                    return false;
            }

            return true;
        }
        catch
        {
            // Invalid CIDR notation
            return false;
        }
    }
}
