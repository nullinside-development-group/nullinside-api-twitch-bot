using Moq;
using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.ChatRules;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

public abstract class AChatRuleUnitTestBase<T> : UnitTestBase where T : AChatRule, new()
{
    /// <summary>
    /// Tests that the message filter is capable of passing at all.
    /// </summary>
    /// <param name="goodString">A friendly string with no issues.</param>
    [Test]
    [TestCase("Hello I love candy and sprinkles")]
    public async Task TestItDoesntAlwaysFail(string goodString)
    {
        var rule = new T();
        var botProxy = new Mock<ITwitchApiProxy>();
        
        // Process the message and assert that we pass the message.
        var chat = new TwitchChatMessage(true, goodString, "123", "456");
        var result = await rule.Handle("123", botProxy.Object, chat, this._db);
        Assert.That(result, Is.True);
        
        // Process the message and assert that we pass the message.
        chat = new TwitchChatMessage(false, goodString, "123", "456");
        result = await rule.Handle("123", botProxy.Object, chat, this._db);
        Assert.That(result, Is.True);
    }
    
    /// <summary>
    /// Tests that the rules are only running when they should be.
    /// </summary>
    [Test]
    public void TestShouldRun()
    {
        var rule = new T();
        
        // Rule is turned on and so is scanning.
        var shouldRun = rule.ShouldRun(new TwitchUserConfig { Enabled = true, BanKnownBots = true });
        Assert.That(shouldRun, Is.True);
        
        // Scanning is turned off
        shouldRun = rule.ShouldRun(new TwitchUserConfig { Enabled = false, BanKnownBots = true });
        Assert.That(shouldRun, Is.False);
        
        // Rule is turned off.
        shouldRun = rule.ShouldRun(new TwitchUserConfig { Enabled = true, BanKnownBots = false });
        Assert.That(shouldRun, Is.False);
        
        // Everything is off.
        shouldRun = rule.ShouldRun(new TwitchUserConfig { Enabled = false, BanKnownBots = false });
        Assert.That(shouldRun, Is.False);
    }
}