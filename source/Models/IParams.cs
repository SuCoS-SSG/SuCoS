using System.Collections.Generic;

namespace SuCoS;

/// <summary>
/// Interface for all classes that will implement a catch-all YAML
/// values.
/// </summary>
public interface IParams
{
    /// <summary>
    /// Recursive dictionary with non-standard values
    /// </summary>
    Dictionary<string, object> Params { get; set; }
}