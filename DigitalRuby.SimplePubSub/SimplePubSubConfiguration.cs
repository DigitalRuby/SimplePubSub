namespace DigitalRuby.SimplePubSub;

/// <summary>
/// Configuration for initializing simple pub sub
/// </summary>
public sealed class SimplePubSubConfiguration
{
    /// <summary>
    /// List of pub/sub providers
    /// </summary>
    public IReadOnlyDictionary<string, PubSubConfiguration>? Providers { get; set; }
}

/// <summary>
/// A configuration for a queue such as azure service bus, amazon mq, rabbit mq, etc.
/// </summary>
public sealed class PubSubConfiguration
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
    /// User name. Sometimes this goes in the connection string instead.
    /// This maps to an AccessKey for some providers like AmazonSqs.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Password. Sometimes this goes in the connection string instead.
    /// This maps to a SecretKey for some providers like AmazonSqs.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use ssl or not. Sometimes this goes in the connection string instead.
    /// Not all providers use/honor this.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Path to ssl certificate.
    /// Not all providers use/honor this.
    /// </summary>
    public string SslCertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Passphrase for ssl certificate is SslCertificatePath is specified.
    /// Not all providers use/honor this.
    /// </summary>
    public string SslCertificatePassphrase { get; set; } = string.Empty;

    /// <summary>
    /// Servers
    /// </summary>
    public string[] Servers { get; set; } = Array.Empty<string>();

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
    /// In memory, not for production use
    /// </summary>
    InMemory = 1,

    /// <summary>
    /// Rabbit mq
    /// </summary>
    RabbitMq = 2,

    /// <summary>
    /// Active mq
    /// </summary>
    ActiveMq = 3,

    /// <summary>
    /// Amazon sqs
    /// </summary>
    AmazonSqs = 4,

    /// <summary>
    /// Azure service bus
    /// </summary>
    AzureServiceBus = 5,

    /// <summary>
    /// Grpc
    /// </summary>
    Grpc = 6
}
