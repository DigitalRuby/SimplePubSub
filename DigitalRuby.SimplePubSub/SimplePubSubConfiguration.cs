namespace DigitalRuby.SimplePubSub;

/// <summary>
/// Configuration for initializing simple pub sub
/// </summary>
public sealed class SimplePubSubConfiguration
{
    /// <summary>
    /// List of pub/sub providers
    /// </summary>
    public IReadOnlyDictionary<string, PubSubProvider>? Providers { get; set; }
}

/// <summary>
/// A provider such as azure service bus, amazon mq, rabbit mq, etc.
/// </summary>
public sealed class PubSubProvider
{
    /// <summary>
    /// Provider key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Provider type
    /// </summary>
    public ProviderType Type { get; set; }

    /// <summary>
    /// Connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Retry intervals, these should be only a few and 5-10 seconds at most
    /// </summary>
    public TimeSpan[]? Retries { get; set; }
    
    /// <summary>
    /// Redelivery intervals
    /// </summary>
    public TimeSpan[]? Redeliveries { get; set; }
}

public enum ProviderType
{
    /// <summary>
    /// Custom provider
    /// </summary>
    Custom = 0,

    /// <summary>
    /// In memory, not for production use
    /// </summary>
    InMemory = 1,

    /// <summary>
    /// Rabbit mq
    /// </summary>
    RabbitMq = 2,

    /// <summary>
    /// Redis
    /// </summary>
    Redis = 3,

    /// <summary>
    /// Active mq
    /// </summary>
    ActiveMq = 4,

    /// <summary>
    /// Amazon sqs
    /// </summary>
    AmazonSqs = 5,

    /// <summary>
    /// Azure service bus
    /// </summary>
    AzureServiceBus = 6,

    /// <summary>
    /// Grpc
    /// </summary>
    Grpc = 7
}