namespace Lib.Test;

using System.Collections.Concurrent;
using System.Reflection;
using FluentAssertions;

public class WordCounterTests
{
    [Fact]
    public async Task Test1()
    {
        var dictionaryFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "words.txt");

        var resultsDictionary = new ConcurrentDictionary<string, int>();

        await WordCounter.RunAsync(
            dictionaryFilePath,
            (kvp) =>
            {
                var word = kvp.Key.ToLowerInvariant();

                if (word.StartsWith("s") && word.Length >= 15)
                {
                    resultsDictionary.TryAdd(kvp.Key, kvp.Value);
                }
            });

        resultsDictionary.Count.Should().Be(3671);
    }
}
