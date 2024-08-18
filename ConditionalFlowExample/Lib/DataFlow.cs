namespace Lib;

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
            var downloadBlock = new TransformBlock<string, string>(async (url) =>
            {
                using (_logger.BeginScope("DownloadBlock"))
                {
                    _logger.LogInformation("Inside download block.");

                    await Task.Delay(TimeSpan.FromSeconds(_random.Next(1, 5)));

                    var content = _faker.Lorem.Sentence(5);

                    _logger.LogInformation("Downloaded {@Content} from {@Url}", content, url);

                    return content;
                }

            }, _executionOptions);

        var storageBlock = new ActionBlock<string>(async (content) =>
        {
            using(_logger.BeginScope("StorageBlock"))
            {
                _logger.LogInformation("Storing {@Content}...", content);

                await Task.Delay(TimeSpan.FromSeconds(_random.Next(1, 5)));

                _logger.LogInformation("Content stored: {@Content}", content);
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
