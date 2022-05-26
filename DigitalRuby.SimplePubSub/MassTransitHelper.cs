namespace DigitalRuby.SimplePubSub;

/// <summary>
/// Extension methods for simple pub/sub
/// </summary>
public static class MassTransitHelper
{
    private const string configPath = "DigitalRuby.SimplePubSub.Configuration";

    private static IServiceProvider? serviceProvider;
    private class MassTransitHelperService : BackgroundService
    {
        public MassTransitHelperService(IServiceProvider provider)
        {
            MassTransitHelper.serviceProvider = provider;
        }

        /// <inheritdoc />
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }

    /// <summary>
    /// Add pub/sub (producer/consumer) functionality to your application
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="namespaceFilterRegex">Regex filter for assembly scanning or null for all assemblies</param>
    public static void AddSimplePubSub(this IServiceCollection services,
        IConfiguration configuration,
        string? namespaceFilterRegex = null)
    {
        services.AddSimpleDi(configuration, namespaceFilterRegex);
        SimplePubSubConfiguration configurationObject = new();
        configuration.Bind(configPath, configurationObject);
        AddSimplePubSub(services, configurationObject, namespaceFilterRegex);
    }

    /// <summary>
    /// Add pub/sub (producer/consumer) functionality to your application
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="namespaceFilterRegex">Regex filter for assembly scanning or null for all assemblies</param>
    public static void AddSimplePubSub(this IServiceCollection services,
        SimplePubSubConfiguration configuration,
        string? namespaceFilterRegex = null)
    {
        if (configuration.Providers is null)
        {
            throw new InvalidOperationException("Null or empty provider in configuration, check config path " + configPath);
        }

        // ugly hack, have not yet figured out how to use IServiceProvider for custom consumers pulled from dependency injection
        // by the time we get inside AddMassTransit, the static IServiceProvider reference will be assigned properly
        serviceProvider = null;
        services.AddHostedService<MassTransitHelperService>();

        // assign keys
        foreach (var provider in configuration.Providers)
        {
            provider.Value.Key = provider.Key;
        }

        // store each registered queue system for producers to use
        QueueSystems queueSystems = new();

        services.AddMassTransit(cfg =>
        {
            foreach (var provider in configuration.Providers.Values)
            {
                switch (provider.Type)
                {
                    case ProviderType.Custom:
                        break;

                    case ProviderType.InMemory:
                        queueSystems.AddQueueSystem(provider.Key, ConfigureInMemory(provider, cfg, namespaceFilterRegex));
                        break;

                    case ProviderType.RabbitMq:
                        break;

                    case ProviderType.Redis:
                        break;

                    case ProviderType.ActiveMq:
                        break;

                    case ProviderType.AmazonSqs:
                        break;

                    case ProviderType.AzureServiceBus:
                        break;
                }
            }
        });

        services.AddSingleton<IQueueSystems>(queueSystems);
        queueSystems.Wait();
    }

    private static void AddConsumers(IBusFactoryConfigurator cfg,
        PubSubProvider pubSubProvider,
        string? namespaceFilterRegex)
    {
        var allTypes = ReflectionHelpers.GetAllTypes(namespaceFilterRegex)
            .Select(t => new
            {
                Type = t,
                Attribute = t.GetCustomAttributes(typeof(ConsumerAttribute), true).FirstOrDefault() as ConsumerAttribute
            })
            .Where(t => t.Attribute is not null &&
            (
                string.IsNullOrWhiteSpace(t.Attribute.QueueName) ||
                t.Attribute.QueueName.Equals(pubSubProvider.Key, StringComparison.OrdinalIgnoreCase))
            )
            .GroupBy(t => t.Attribute!.GetQueueName(t.Type))
            .ToArray();

        foreach (var type in allTypes)
        {
            bool temporary = false;

#if DEBUG

            // force temporary for debugging purposes, don't want to create gobs of queues
            if (Debugger.IsAttached)
            {
                temporary = true;
            }

#endif

            void ConfigureReceiveEndPoint(IReceiveEndpointConfigurator endPointCfg)
            {
                if (pubSubProvider.Retries is not null && pubSubProvider.Retries.Length != 0)
                {
                    endPointCfg.UseRetry(r => r.Intervals(pubSubProvider.Retries));
                }
                if (pubSubProvider.Redeliveries is not null && pubSubProvider.Redeliveries.Length != 0)
                {
                    endPointCfg.UseMessageRetry(r => r.Intervals(pubSubProvider.Redeliveries));
                }
                foreach (var consumer in type)
                {
                    if (consumer.Attribute!.PrefetchCount > 0)
                    {
                        endPointCfg.PrefetchCount = Math.Max(endPointCfg.PrefetchCount, consumer.Attribute!.PrefetchCount);
                    }
                    endPointCfg.Consumer(consumer.Type, _type =>
                    {
                        if (serviceProvider is null)
                        {
                            throw new ApplicationException("Fatal: unable to retrieve IServiceProvider");
                        }
                        return serviceProvider!.GetRequiredService(consumer.Type);
                    });
                }
                /*
                if (temporary)
                {
                    // HACK: Mass transit does not expose a property on the generic end point config to denote the queue is temporary
                    // so we use some of the common properties on various queue providers
                    TrySetProperty(endPointCfg, "Durable", false);
                    TrySetProperty(endPointCfg, "AutoDelete", true);
                    TrySetProperty(endPointCfg, "Exclusive", true);
                    TrySetProperty(endPointCfg, "AutoDeleteOnIdle", TimeSpan.FromMinutes(1.0));
                }
                */
            }
            if (temporary)
            {
                cfg.ReceiveEndpoint(ConfigureReceiveEndPoint);
            }
            else
            {
                cfg.ReceiveEndpoint(type.Key, ConfigureReceiveEndPoint);
            }
        }
    }

    private static IBusControl ConfigureInMemory(PubSubProvider provider, IBusRegistrationConfigurator cfg, string? namespaceFilterRegex)
    {
        return Bus.Factory.CreateUsingInMemory(cfg => AddConsumers(cfg, provider, namespaceFilterRegex));
    }

    /*
    private static void TrySetProperty(object obj, string propertyName, object propertyValue)
    {
        var info = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
        if (info is not null)
        {
            info.SetValue(obj, propertyValue);
        }
    }
    */
}