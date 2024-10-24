using Moq;
using Nullinside.Api.Common.Twitch;
using Nullinside.Api.TwitchBot.ChatRules;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

public class StreamViewersTests : AChatRuleUnitTestBase<StreamViewers>
{
    [Test]
    [TestCase("@jellynyeko dо уоu alrеady triеd strеamviewers  оrg? Real viewеrs, fire works! Тhеy arе now giving оut а frее рackagе for streamers оО")]
    [TestCase("@kygaming98 dо уоu аlready tried streаmviewers  оrg? Real viewers, firе works! Thеy arе now giving оut а freе package fоr streamers oО")]
    public async Task TestKnownStrings(string badString)
    {
        var rule = new StreamViewers();
        var botProxy = new Mock<ITwitchApiProxy>();
        var chat = new TwitchChatMessage(true, badString, "123", "456");
        
        // Process the message and assert that we fail the message.
        var result = await rule.Handle("123", botProxy.Object, chat, this._db);
        Assert.That(result, Is.False);
    }
}