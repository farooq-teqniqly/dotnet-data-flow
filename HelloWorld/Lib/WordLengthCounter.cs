namespace Lib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public class WordLengthCounter
{
    protected WordLengthCounter()
    {

    }

    public static async Task RunAsync(
        string dictionaryFilePath,
        Action<KeyValuePair<int, int>> resultHandler)
    {
        var aggregationResults = new ConcurrentDictionary<int, int>();

        var importBlock = new TransformBlock<string, IEnumerable<string>>(
            async p => await File.ReadAllLinesAsync(p));

        var aggregationBlock = new TransformManyBlock<IEnumerable<string>, KeyValuePair<int, int>>(lines =>
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var key = line.Trim().Length;

                aggregationResults.AddOrUpdate(
                    key,
                    1,
                    (k, _) => aggregationResults[k] + 1);
            }

            return aggregationResults;

        },
            new ExecutionDataflowBlockOptions{MaxDegreeOfParallelism = 2});

        var notifyBlock = new ActionBlock<KeyValuePair<int, int>>(
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
