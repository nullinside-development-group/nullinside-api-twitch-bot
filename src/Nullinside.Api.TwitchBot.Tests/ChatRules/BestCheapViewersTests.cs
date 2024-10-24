using Moq;
using Nullinside.Api.Common.Twitch;
using Nullinside.Api.TwitchBot.ChatRules;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

/// <summary>
///     Tests the <see cref="BestCheapViewers" /> class.
/// </summary>
public class BestCheapViewersTests : AChatRuleUnitTestBase<BestCheapViewers>
{
    /// <summary>
    ///     Tests strings that have been typed in chats.
    /// </summary>
    /// <param name="badString">The string that should fail.</param>
    [Test]
    [TestCase("Best viewers on ***")]
    [TestCase("Best  viewers on ***")]
    [TestCase("Best vie̮wers on ***")]
    [TestCase("Best́ viewers on ***")]
    [TestCase("Be̩st Viewers on ***")]
    [TestCase("Be̾st Viewers on ***")]
    [TestCase("B͟est Viewers on ***")]
    [TestCase("B̟est viewers on ***")]
    [TestCase("Cheap viewers on ***")]
    [TestCase("Che̢ap vie̮wers on ***")]
    [TestCase("Ch̍eap Viewers on ***")]
    [TestCase("Ch͟eap viewers on ***")]
    [TestCase("C̀heap Viewers on ***")]
    [TestCase("Cheaͧp v̫iewers on ***")]
    [TestCase("Cheaͧp v̫iewers on *** ")]
    public async Task TestKnownStrings(string badString)
    {
        var rule = new BestCheapViewers();
        var botProxy = new Mock<ITwitchApiProxy>();
        var chat = new TwitchChatMessage(true, badString, "123", "456");

        // Process the message and assert that we fail the message.
        var result = await rule.Handle("123", botProxy.Object, chat, _db);
        Assert.That(result, Is.False);
    }
}