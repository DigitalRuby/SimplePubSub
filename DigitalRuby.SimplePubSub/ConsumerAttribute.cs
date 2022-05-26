namespace DigitalRuby.SimplePubSub;

/// <summary>
/// Apply this attribute to consumers to configure queue properties
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ConsumerAttribute : BindingAttribute
{
	/// <summary>
	/// The provider key to consume from or empty string for all
	/// </summary>
	public string ProviderKey { get; } = string.Empty;

	/// <summary>
	/// Queue name. Null/empty to infer from class name.
	/// </summary>
	public string? QueueName { get; } = string.Empty;

	/// <summary>
	/// Override the default prefetch message count if desired, 0 for default
	/// </summary>
	public int PrefetchCount { get; }

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="providerKey">Provider key, empty string to consume all possible queue providers</param>
	/// <param name="scope">Service lifetime</param>
	public ConsumerAttribute(string providerKey = "",
		ServiceLifetime scope = ServiceLifetime.Singleton)
		: base(scope)
	{
		ProviderKey = providerKey;
	}

	/// <summary>
	/// Get a queue name
	/// </summary>
	/// <param name="type">Type</param>
	/// <returns>Queue name</returns>
	public string GetQueueName(Type type)
	{
		if (string.IsNullOrWhiteSpace(QueueName))
		{
			return type.FullName!;
		}
		return QueueName;
	}
}
