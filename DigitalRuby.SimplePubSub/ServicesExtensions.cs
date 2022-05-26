using System.Linq;

namespace DigitalRuby.SimplePubSub;

/// <summary>
/// Extension methods for simple pub/sub
/// </summary>
public static class ServicesExtensions
{
    private const string configPath = "DigitalRuby.SimplePubSub.Configuration";

    private class Resolver
    {
        public IServiceProvider? Provider { get; set; }
    }

    /// <summary>
    /// Ensures that we don't double-call any UseSimplePubSub types
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentBag<object> objectsCalledInUseSimplePubSub = new();

    private class MassTransitHelperService : BackgroundService
    {
        public MassTransitHelperService(IServiceProvider provider, Resolver resolver)
        {
            resolver.Provider = provider;
        }

        /// <inheritdoc />
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ServicesExtensions.objectsCalledInUseSimplePubSub.Clear();
            return Task.CompletedTask;
        }
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
        if (services.SimplePubSubAdded())
        {
            return;
        }
        else if (configuration.Providers is null || configuration.Providers.Count == 0)
        {
            throw new InvalidOperationException("Null or empty provider in configuration, check config path " + configPath);
        }

        // take advantage of the fact that hosted services are created up front and as such we can grab an IServiceProvider reference for use
        //  by the consumer creation code later
        Resolver resolver = new();
        services.AddSingleton(resolver);
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
                IBusControl busControl = provider.Type switch
                {
                    ProviderType.InMemory => ConfigureInMemory(resolver, provider, namespaceFilterRegex),
                    ProviderType.RabbitMq => ConfigureRabbitMq(resolver, provider, namespaceFilterRegex),
                    ProviderType.ActiveMq => ConfigureActiveMq(resolver, provider, namespaceFilterRegex),
                    ProviderType.AmazonSqs => ConfigureAmazonSqs(resolver, provider, namespaceFilterRegex),
                    ProviderType.AzureServiceBus => ConfigureAzureServiceBus(resolver, provider, namespaceFilterRegex),
                    ProviderType.Grpc => ConfigureGrpc(resolver, provider, namespaceFilterRegex),
                    _ => throw new ArgumentException($"Provider type {provider.Type} is not supported"),
                };
                queueSystems.AddQueueSystem(provider.Key, busControl);
            }
        });

        services.AddSingleton<IQueueSystems>(queueSystems);
        queueSystems.Wait();
    }

    /// <summary>
    /// Use simple pub sub in web application
    /// </summary>
    /// <param name="services">Services</param>
    /// <param name="appBuilder">App builder</param>
    public static void UseSimplePubSub(this IServiceCollection services, Microsoft.AspNetCore.Builder.IApplicationBuilder appBuilder)
    {
        _ = services;
        if (!objectsCalledInUseSimplePubSub.Contains(appBuilder))
        {
            objectsCalledInUseSimplePubSub.Add(appBuilder);

            // Any further setup can go here
        }
    }

    /// <summary>
    /// Determine if simple pub sub was already added to services
    /// </summary>
    /// <param name="services">Services</param>
    /// <returns>True if simple pub sub is added, false otherwise</returns>
    public static bool SimplePubSubAdded(this IServiceCollection services)
    {
        return (services.Any(s => s.ImplementationType == typeof(MassTransitHelperService)));
    }

    private static void AddConsumers(Resolver resolver,
        IBusFactoryConfigurator cfg,
        PubSubConfiguration pubSubProvider,
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
                string.IsNullOrWhiteSpace(t.Attribute.ProviderKey) ||
                t.Attribute.ProviderKey.Equals(pubSubProvider.Key, StringComparison.OrdinalIgnoreCase))
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
                        if (resolver?.Provider is null)
                        {
                            throw new ApplicationException("Fatal: unable to retrieve IServiceProvider");
                        }
                        return resolver.Provider.GetRequiredService(consumer.Type);
                    });
                }
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

    private static IBusControl ConfigureInMemory(Resolver resolver, PubSubConfiguration provider, string? namespaceFilterRegex)
    {
        return Bus.Factory.CreateUsingInMemory(cfg =>
        {
            AddConsumers(resolver, cfg, provider, namespaceFilterRegex);
        });
    }

    private static IBusControl ConfigureRabbitMq(Resolver resolver, PubSubConfiguration provider, string? namespaceFilterRegex)
    {
        return Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(provider.ConnectionString, innerCfg =>
            {
                if (!string.IsNullOrWhiteSpace(provider.UserName))
                {
                    innerCfg.Username(provider.UserName);
                }
                if (!string.IsNullOrWhiteSpace(provider.Password))
                {
                    innerCfg.Password(provider.Password);
                }
                if (provider.UseSsl)
                {
                    innerCfg.UseSsl(sslCfg =>
                    {
                        if (!string.IsNullOrWhiteSpace(provider.SslCertificatePath))
                        {
                            sslCfg.CertificatePath = provider.SslCertificatePath;
                        }
                        if (!string.IsNullOrWhiteSpace(provider.SslCertificatePassphrase))
                        {
                            sslCfg.CertificatePassphrase = provider.SslCertificatePassphrase;
                        }
                    });
                }
            });
            AddConsumers(resolver, cfg, provider, namespaceFilterRegex);
        });
    }

    private static IBusControl ConfigureActiveMq(Resolver resolver, PubSubConfiguration provider, string? namespaceFilterRegex)
    {
        return Bus.Factory.CreateUsingActiveMq(cfg =>
        {
            cfg.Host(new Uri(provider.ConnectionString), innerCfg =>
            {
                if (!string.IsNullOrWhiteSpace(provider.UserName) &&
                    !string.IsNullOrWhiteSpace(provider.Password))
                {
                    innerCfg.Username(provider.UserName);
                    innerCfg.Password(provider.Password);
                }
                if (provider.UseSsl)
                {
                    innerCfg.UseSsl();
                }
            });
            AddConsumers(resolver, cfg, provider, namespaceFilterRegex);
        });
    }

    private static IBusControl ConfigureAmazonSqs(Resolver resolver, PubSubConfiguration provider, string? namespaceFilterRegex)
    {
        return Bus.Factory.CreateUsingAmazonSqs(cfg =>
        {
            cfg.Host(provider.ConnectionString, innerCfg =>
            {
                if (!string.IsNullOrWhiteSpace(provider.UserName) &&
                    !string.IsNullOrWhiteSpace(provider.Password))
                {
                    innerCfg.AccessKey(provider.UserName);
                    innerCfg.SecretKey(provider.Password);
                }
            });
            AddConsumers(resolver, cfg, provider, namespaceFilterRegex);
        });
    }

    private static IBusControl ConfigureAzureServiceBus(Resolver resolver, PubSubConfiguration provider, string? namespaceFilterRegex)
    {
        return Bus.Factory.CreateUsingAzureServiceBus(cfg =>
        {
            cfg.Host(provider.ConnectionString, innerCfg =>
            {
            });
            AddConsumers(resolver, cfg, provider, namespaceFilterRegex);
        });
    }

    private static IBusControl ConfigureGrpc(Resolver resolver, PubSubConfiguration provider, string? namespaceFilterRegex)
    {
        return Bus.Factory.CreateUsingGrpc(cfg =>
        {
            cfg.Host(new Uri(provider.ConnectionString), innerCfg =>
            {
                foreach (var server in provider.Servers)
                {
                    innerCfg.AddServer(new Uri(server));
                }
            });
            AddConsumers(resolver, cfg, provider, namespaceFilterRegex);
        });
    }
}