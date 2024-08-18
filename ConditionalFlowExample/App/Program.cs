namespace App;

using Bogus;
using Lib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Templates.Themes;

internal class Program
{
    protected Program()
    {

    }

    static async Task Main()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(_ => new Faker());
        builder.Services.AddSingleton<DataFlow>();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);

        var host = builder.Build();

        using (host)
        {
            var faker = host.Services.GetRequiredService<Faker>();

            var urls = Enumerable.Range(1, 5)
                .Select(_ => faker.Internet.Url())
                .ToList();

            var datFlow = host.Services.GetRequiredService<DataFlow>();
            await datFlow.RunAsync(urls);
        }
    }
}
