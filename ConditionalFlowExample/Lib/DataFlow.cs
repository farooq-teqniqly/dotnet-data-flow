namespace Lib;

using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using Bogus;
using Microsoft.Extensions.Logging;

public class DataFlow
{
    private readonly ILogger<DataFlow> _logger;
    private readonly Faker _faker;
    private readonly Random _random = new();

    private static readonly ExecutionDataflowBlockOptions _executionOptions = new()
    {
        MaxDegreeOfParallelism = 2
    };

    private static readonly DataflowLinkOptions _linkOptions = new() { PropagateCompletion = true };
    public DataFlow(ILogger<DataFlow> logger, Faker faker)
    {
        _logger = logger;
        _faker = faker;
    }

    public async Task RunAsync(IEnumerable<string> urls)
    {
            var downloadBlock = new TransformBlock<string, (string, string)>(async (url) =>
            {
                using (var activity = new Activity("Download block"))
                {
                    activity.SetTag("Url", url);
                    activity.Start();

                    await Task.Delay(TimeSpan.FromSeconds(_random.Next(1, 5)));
                    var content = _faker.Lorem.Sentence(5);
                    return (url, content);
                }
            }, _executionOptions);

        var storageBlock = new ActionBlock<(string, string)>(async (tup) =>
        {
            var url = tup.Item1;
            var content = tup.Item2;

            using (var activity = new Activity("Storage block"))
            {
                activity.SetTag("Url", url);
                activity.SetTag("ContentLength", content.Length);

                activity.Start();
                await Task.Delay(TimeSpan.FromSeconds(_random.Next(1, 5)));
            }

        }, _executionOptions);

        downloadBlock.LinkTo(storageBlock, _linkOptions);

        foreach (var url in urls)
        {
            _logger.LogInformation("Downloading content from {@Url}...", url);

            await downloadBlock.SendAsync(url);
        }

        downloadBlock.Complete();

        await Task.WhenAll(
            downloadBlock.Completion,
            storageBlock.Completion);

        _logger.LogInformation("Done.");
    }
}
