namespace Lib;

using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using static System.Net.WebRequestMethods;

public class DataFlow
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEnumerable<HttpRequestConfiguration> _configurations;
    private readonly ILogger<DataFlow> _logger;

    public DataFlow(
        IHttpClientFactory httpClientFactory,
        IEnumerable<HttpRequestConfiguration> configurations,
        ILogger<DataFlow> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configurations = configurations;
        _logger = logger;
    }

    public async Task RunAsync(Action<string> handleResponse)
    {
        var dataFlowBlockOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2 };

        var downloadBlock = new TransformBlock<HttpRequestConfiguration, string>(async (config) =>
        {
            using (var httpClient = _httpClientFactory.CreateClient(config.ClientName))
            {
                var request = new HttpRequestMessage(config.Method, config.Uri);
                var response = await httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
        }, dataFlowBlockOptions);

        var notifyBlock = new ActionBlock<string>((handleResponse), dataFlowBlockOptions);

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        downloadBlock.LinkTo(notifyBlock, linkOptions);

        foreach (var configuration in _configurations)
        {
            _logger.LogInformation("Downloading ");
            await downloadBlock.SendAsync(configuration);
        }

        downloadBlock.Complete();

        await Task.WhenAll(
            downloadBlock.Completion,
            notifyBlock.Completion);
    }
}
