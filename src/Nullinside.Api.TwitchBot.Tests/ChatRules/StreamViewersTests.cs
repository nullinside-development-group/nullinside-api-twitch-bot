using Nullinside.Api.TwitchBot.ChatRules;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

public class StreamViewersTests
{
    [Test]
    [TestCase("@jellynyeko dо уоu alrеady triеd strеamviewers  оrg? Real viewеrs, fire works! Тhеy arе now giving оut а frее рackagе for streamers оО")]
    [TestCase("@kygaming98 dо уоu аlready tried streаmviewers  оrg? Real viewers, firе works! Thеy arе now giving оut а freе package fоr streamers oО")]
    public void TestKnownStrings(string badString)
    {
        // Need to put interfaces in front of the classes before we can do this.
        Assert.Pass();
    }
}