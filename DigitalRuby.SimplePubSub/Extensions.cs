namespace DigitalRuby.SimplePubSub;

/// <summary>
/// Extension methods for simple pub/sub
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Add pub/sub (producer/consumer) functionality to your application
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="namespaceFilterRegex">Regex filter for assembly scanning or null for all assemblies</param>
    public static void AddSimplePubSub(this IServiceCollection services, IConfiguration configuration, string? namespaceFilterRegex = null)
    {
        services.AddSimpleDi(configuration, namespaceFilterRegex);
    }
}