namespace QrCodeGenerator.Tests;

public class ValidationAndBufferTests
{
    private const string Twelve = "glory promote mansion submit captain finger virus verb utility general welcome flush";
    private const string TwentyFour = "future alarm discover sustain maple keen shrimp uncle wagon tray puzzle ring embrace velvet elegant robust census provide layer decorate cause foam pride timber";

    [Theory]
    [InlineData(Twelve)]
    [InlineData(TwentyFour)]
    public void AcceptsValidSeedPhrases(string phrase) => SeedPhraseManager.ValidateSeedPhrase(phrase);

    [Theory]
    [InlineData("glory  promote mansion submit captain finger virus verb utility general welcome flush")] // double space
    [InlineData(" glory promote mansion submit captain finger virus verb utility general welcome flush")] // leading
    [InlineData("glory promote mansion submit captain finger virus verb utility general welcome flush ")] // trailing
    [InlineData("glory promote mansion submit captain finger virus verb utility general welcome")]        // 11 words
    [InlineData("glory promote mansion submit captain finger virus verb utility general welcome zzzzqx")] // invalid word
    [InlineData("Glory promote mansion submit captain finger virus verb utility general welcome flush")]  // uppercase
    public void RejectsInvalidSeedPhrasesWithoutEchoingWords(string phrase)
    {
        var ex = Assert.Throws<ArgumentException>(() => SeedPhraseManager.ValidateSeedPhrase(phrase));
        foreach (string word in phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            Assert.DoesNotContain(word, ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void SecretBufferIsZeroAfterDispose()
    {
        var buffer = new SecretBuffer(8);
        foreach (char c in "secret") buffer.Append(c);
        Assert.Equal("secret", buffer.Span.ToString());

        buffer.Dispose();

        Assert.Equal(0, buffer.Length);
        Assert.All(buffer.Backing.ToArray(), c => Assert.Equal('\0', c));
    }

    [Fact]
    public void SecretBufferIgnoresAppendPastCapacity()
    {
        using var buffer = new SecretBuffer(3);
        Assert.True(buffer.Append('a'));
        Assert.True(buffer.Append('b'));
        Assert.True(buffer.Append('c'));
        Assert.False(buffer.Append('d'));
        Assert.Equal("abc", buffer.Span.ToString());
    }

    [Fact]
    public void SecretBufferRemoveLastErasesChar()
    {
        using var buffer = new SecretBuffer(3);
        buffer.Append('a');
        buffer.Append('b');
        Assert.True(buffer.RemoveLast());
        Assert.Equal("a", buffer.Span.ToString());
        Assert.Equal('\0', buffer.Backing[1]);
    }
}
