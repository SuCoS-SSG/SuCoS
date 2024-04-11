using SuCoS.Models;

namespace SuCoS.TemplateEngine;

/// <summary>
/// Interface for all Templace Engines (Liquid)
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Initialization
    /// </summary>
    /// <param name="site"></param>
    void Initialize(Site site);

    /// <summary>
    /// Parse the content using the data template
    /// </summary>
    /// <param name="template"></param>
    /// <param name="site"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    string Parse(string template, ISite site, IPage page);

    /// <summary>
    /// Parse the content using the data template for Resource naming
    /// </summary>
    /// <param name="template"></param>
    /// <param name="site"></param>
    /// <param name="page"></param>
    /// <param name="counter"></param>
    /// <returns></returns>
    string? ParseResource(string? template, ISite site, IPage page, int counter);
}
