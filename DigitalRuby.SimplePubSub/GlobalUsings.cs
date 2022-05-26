global using System.Reflection;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

global using DigitalRuby.SimpleDi;

global using MassTransit;
global using MassTransit.Consumer;

global using MassTransit.ActiveMqTransport;
global using MassTransit.AmazonSqsTransport;
global using MassTransit.AzureServiceBusTransport;
global using MassTransit.InMemoryTransport;
global using MassTransit.RabbitMqTransport;

global using System.Diagnostics;

namespace DigitalRuby.SimplePubSub;

