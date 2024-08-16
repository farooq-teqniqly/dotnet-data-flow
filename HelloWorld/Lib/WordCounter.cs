namespace Lib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public class WordCounter
{
    public static async Task RunAsync(
        string dictionaryFilePath,
        Action<KeyValuePair<string, int>> resultHandler)
    {
        var aggregationResults = new ConcurrentDictionary<string, int>();

        var importBlock = new TransformBlock<string, IEnumerable<string>>(
            async p => await File.ReadAllLinesAsync(p));

        var aggregationBlock = new TransformManyBlock<IEnumerable<string>, KeyValuePair<string, int>>(lines =>
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                aggregationResults.AddOrUpdate(
                    line,
                    1,
                    (l, _) => aggregationResults[l] + 1);
            }

            return aggregationResults;

        },
            new ExecutionDataflowBlockOptions{MaxDegreeOfParallelism = 2});

        var notifyBlock = new ActionBlock<KeyValuePair<string, int>>(
            resultHandler,
            new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 2});

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        importBlock.LinkTo(aggregationBlock, linkOptions);
        aggregationBlock.LinkTo(notifyBlock, linkOptions);

        await importBlock.SendAsync(dictionaryFilePath);
        importBlock.Complete();

        await Task.WhenAll(
            importBlock.Completion,
            aggregationBlock.Completion,
            notifyBlock.Completion);
    }
}
