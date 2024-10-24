using Moq;
using Nullinside.Api.Common.Twitch;
using Nullinside.Api.TwitchBot.ChatRules;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

public class DiscordTests : AChatRuleUnitTestBase<Discord>
{
    [Test]
    [TestCase("Hello mate you stream pretty cool that's why I follow I would love to be a fan of yours if you don't mind kindly add me on Discord: 👉👉lacaster5")]
    [TestCase("Hello, I just recently found your channel and can already tell that your content is great, and I would love to stick with you long term. If you're open to it, I'd be willing to connect with you on Discord. My username is teecash1000")]
    [TestCase("Hey there 👋 You stream pretty cool that’s why I followed you. I will like to make you a friend and be a fan, if you don’t mind Kindly chat me on Discord, my Discord username is tuckzay")]
    [TestCase("Hi! Just wanted to say that I absolutely love your gameplay and content.l'd love to connect better with you on Discord if that's possible. Looking forward to more awesome streams from you! My username is 👉👉👉 edisonpires")]
    [TestCase("What's up Friend, great stream! I'm having a blast watching you stream. Let's move the conversation to Discord, where we can discuss more about streaming in more detail and get to know each other better. See you there! My discord username is 👉john_6029")]
    public async Task TestKnownStrings(string badString)
    {
        var rule = new Discord();
        var botProxy = new Mock<ITwitchApiProxy>();
        var chat = new TwitchChatMessage(true, badString, "123", "456");
        
        // Process the message and assert that we fail the message.
        var result = await rule.Handle("123", botProxy.Object, chat, this._db);
        Assert.That(result, Is.False);
    }
    
    [Test]
    [TestCase("I've heard of the application discord before and it sounds great")]
    [TestCase("I was talking on my discord the other day")]
    public async Task EnsureNoFalsePositives(string badString)
    {
        var rule = new Discord();
        var botProxy = new Mock<ITwitchApiProxy>();
        var chat = new TwitchChatMessage(true, badString, "123", "456");
        
        // Process the message and assert that we do not fail the message.
        var result = await rule.Handle("123", botProxy.Object, chat, this._db);
        Assert.That(result, Is.True);
    }
}