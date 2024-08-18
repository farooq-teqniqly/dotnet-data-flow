namespace App;

using Bogus;
using Lib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Templates.Themes;
using SerilogTracing;
using SerilogTracing.Expressions;

internal class Program
{
    protected Program()
    {

    }

    static async Task Main()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddUserSecrets<Program>();

        builder.Services.AddSingleton(_ => new Faker());
        builder.Services.AddSingleton<DataFlow>();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(Formatters.CreateConsoleTextFormatter(TemplateTheme.Code))
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

            using var listener = new ActivityListenerConfiguration().TraceToSharedLogger();
            {
                using (var activity = Log.Logger.StartActivity("Run DataFlow"))
                {
                    var datFlow = host.Services.GetRequiredService<DataFlow>();
                    await datFlow.RunAsync(urls);
                    activity.Complete();
                }
            }
        }

        await Log.CloseAndFlushAsync();
    }
}
