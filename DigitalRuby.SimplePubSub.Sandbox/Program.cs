// playground for testing pub-sub

Console.WriteLine("Setting up...");
var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((context, services) =>
{
    services.AddSimplePubSub(context.Configuration);
});

Console.WriteLine("Building...");
var host = builder.Build();

Console.WriteLine("Running... Ctrl-C to quit");
await host.RunAsync();

