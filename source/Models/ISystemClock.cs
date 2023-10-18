namespace SuCoS.Models;

/// <summary>
/// Represents an interface for accessing the system clock.
/// </summary>
public interface ISystemClock
{
    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the current Coordinated Universal Time (UTC).
    /// </summary>
    DateTime UtcNow { get; }
}

/// <summary>
/// Represents a concrete implementation of the ISystemClock interface using the system clock.
/// </summary>
public class SystemClock : ISystemClock
{
    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    public DateTime Now => DateTime.Now;

    /// <summary>
    /// Gets the current Coordinated Universal Time (UTC).
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}
