using System.Net.NetworkInformation;
using Serilog;

namespace SuCoS.Commands;

/// <summary>
/// Default implementation of port selection
/// </summary>
public class DefaultPortSelector(ILogger logger) : IPortSelector
{
    /// <summary>
    /// Check if a given port is available, if not, search for another available.
    /// </summary>
    /// <param name="baseUrl"></param>
    /// <param name="initialPort"></param>
    /// <param name="maxTries"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public int SelectAvailablePort(string baseUrl, int initialPort, int maxTries)
    {
        for (var attempt = 0; attempt < maxTries; attempt++)
        {
            var portToTry = initialPort + attempt;

            if (IsPortAvailable(portToTry))
            {
                return portToTry;
            }

            logger.Warning($"Port {portToTry} is not available. Trying next port.");
        }

        throw new InvalidOperationException(
            $"Could not find an available port after {maxTries} attempts starting from {initialPort}.");
    }

    private static bool IsPortAvailable(int port)
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var activeTcpConnections = properties.GetActiveTcpConnections();
        var activeTcpListeners = properties.GetActiveTcpListeners();

        return !activeTcpConnections.Any(conn => conn.LocalEndPoint.Port == port)
               && !activeTcpListeners.Any(endpoint => endpoint.Port == port);
    }
}
