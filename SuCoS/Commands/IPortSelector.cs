namespace SuCoS.Commands;

/// <summary>
/// Interface for port selection strategy
/// </summary>
public interface IPortSelector
{
    /// <summary>
    /// Selects an available port, trying alternative ports if needed
    /// </summary>
    /// <param name="baseUrl">Base URL to bind</param>
    /// <param name="initialPort">Initially requested port</param>
    /// <param name="maxTries">Maximum number of port selection attempts</param>
    /// <returns>An available port number</returns>
    int SelectAvailablePort(string baseUrl, int initialPort, int maxTries);
}
