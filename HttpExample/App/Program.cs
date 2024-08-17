namespace App;

using Lib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Program
{
    protected Program()
    {
        
    }
    static async Task Main()
    {
        var builder = Host.CreateApplicationBuilder();

        var loggerFactory = LoggerFactory.Create(
            b => b.AddConsole());

        var logger = loggerFactory.CreateLogger<Program>();

        var baseAddress = new Uri(builder.Configuration["BaseAddress"]!);

        var httpConfigurations = new List<HttpRequestConfiguration>
        {
            new(
                "posts",
                new Uri(baseAddress, "posts"),
                HttpMethod.Get),

            new(
                "comments",
                new Uri(baseAddress, "comments"),
                HttpMethod.Get),

            new(
                "users",
                new Uri(baseAddress, "users"),
                HttpMethod.Get),
        };

        foreach (var httpConfiguration in httpConfigurations)
        {
            builder.Services
                .AddHttpClient(httpConfiguration.ClientName)
                .AddStandardResilienceHandler();
        }

        builder.Services.AddSingleton(httpConfigurations.AsEnumerable());
        builder.Services.AddSingleton<DataFlow>();

        var host = builder.Build();

        using (host)
        {
            var dataFlow = host.Services.GetRequiredService<DataFlow>();

            await dataFlow.RunAsync(
                response => logger.LogInformation(
                    "Received response with length {@Length}",
                    response.Length.ToString()));
        }
    }
}
