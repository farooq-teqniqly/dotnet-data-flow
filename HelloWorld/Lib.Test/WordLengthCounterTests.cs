namespace Lib.Test;

using System.Collections.Concurrent;
using System.Reflection;
using FluentAssertions;

public class WordLengthCounterTests
{
    [Fact]
    public async Task Test1()
    {
        var dictionaryFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "words.txt");

        var wordLengthsDictionary = new ConcurrentDictionary<int, int>();

        await WordLengthCounter.RunAsync(
            dictionaryFilePath,
            (kvp) => wordLengthsDictionary.TryAdd(
                kvp.Key,
                kvp.Value));

        wordLengthsDictionary.Should().HaveCount(31);
    }
}
