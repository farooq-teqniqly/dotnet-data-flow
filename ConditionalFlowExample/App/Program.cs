namespace App;

using Bogus;
using Lib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal class Program
{
    protected Program()
    {

    }

    static async Task Main()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices(s =>
        {
            s.AddSingleton(_ => new Faker());
            s.AddSingleton<DataFlow>();
        });

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
