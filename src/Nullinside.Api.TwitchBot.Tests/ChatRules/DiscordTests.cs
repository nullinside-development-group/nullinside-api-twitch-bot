using Moq;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.TwitchBot.ChatRules;
using Nullinside.Api.TwitchBot.Model;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

/// <summary>
///   Tests the <see cref="Discord" /> class.
/// </summary>
public class DiscordTests : AChatRuleUnitTestBase<Discord> {
  /// <summary>
  ///   Tests the strings typed in chats.
  /// </summary>
  /// <param name="badString">The string that should fail.</param>
  [Test]
  [TestCase("Add me on Discord — maybe we create something special together  👉  janetscot")]
  [TestCase("Add me on DISCORD :    ghost_clude")]
  [TestCase("Add me on discord at mactobex")]
  [TestCase("ANELE HELLO STREAMER🫵 just came across your stream and dropped a follow before heading out. I respect the hustle. I actually know a big Twitch streamer with around 808.5K followers who help streamers grow with real  viewers, active chat, and supporters who donate and sub regularly If you're serious about taking your channel to the next level add him on Discord:     DATTODESTINY  and let him know that🫴 henry   sent you Opportunities like this don’t come around often Yagoo")]
  [TestCase("Hello 👋dude you stream pretty good that's why I followed you and would love to catch up on your next stream here is my discord handle: manking78")]
  [TestCase("Hello dude, i love your content vibes i will  be joining next stream with friends let connect on discord >>>  bhavilla")]
  [TestCase("Hey 👑 Your stream is really fun to watch! I dropped a follow to show support 💜 If you have a Discord community, I’d love to be part of it. My Discord is     onabey_gaming")]
  [TestCase("Hey bro I'd love to support your journey. I can connect you with a top Twitch streamer who streams to thousands, has 2m   followers, and earns big. He helps streamers gain organic viewers, active chatters, and loyal fans who donate, gift, and sub. If you're serious about growth, add him on Discord: osulive_5 and tell him SAMSON sent y.          ou. Approach him with respect, he's a big name. Don't wait,              growth starts when you take action.")]
  [TestCase("Hey bro, I'd love to support your journey. I can connect you with a top Twitch streamer who streams to thousands, has   351.9K     followers, and earns a substantial income. He helps streamers gain organic viewers, active chatters, and loyal fans who donate, gift, and subscribe. If you are serious about growth, add him on Discord: 👉        TANG_OTEK23   and tell him PLENTY sent you. Approach him with respect, as he is a well-known figure. Don't wait, growth starts when you take action")]
  [TestCase("Hey bro, just wanted to show some real support for your stream. I can connect you with a top Twitch streamer who streams to thousands, has 118.1k  followers and earns big. He helps smaller streamers grow with organic viewers, active chatters, and loyal fans who might donate, gift, and sub consistently. If you are interested, add him on Discord   CROKEY2   and let him know that QUOD sent you. Approach with respect,       he is a big name, and taking this step could seriously")]
  [TestCase("If you’re up for it, we could talk on Discord sometime 😊 My username is  Janetscot")]
  [TestCase("Let’s stay connected — add me on Discord 👉 janetghost")]
  [TestCase("Yo 🤗 I just stopped by your stream. i couldn't stay long, but I dropped a follow to support your journey. I can connect you with a big Twitch streamer who pulls thousands of viewers, has about 60k followers, and makes serious bank. He helps streamers grow with real viewers, active chatters, and loyal fans who donate, gift, and sub consistently. If you’re serious about growth, add him on Discord:  insighton_esports and tell him deematrix sent you. Growt0h starts when you take action")]
  [TestCase("Yo bro, I just came across your stream and dropped a follow before heading out I respect the hustle. I actually know a big Twitch streamer with thousands of followers who helps streamers grow with real  viewers, active chat, and supporters who donate and sub regularly If you're serious about taking your channel to the next level add him on Discord   GOTRY20  and tell him Lordstb_ sent you Opportunities like this don’t come around often")]
  [TestCase("Yo bro, just caught your stream, can't stay long so I dropped a follow. I wanna support your journey. I can connect you with a big Twitch streamer who pulls thousands of viewers, has about 2million followers, and makes serious bank. He helps streamers grow with real viewers, active chatters, and loyal fans who donate, gift, and sub consistently. If you’re serious about growth, add him on Discord: SEBFORSEN  and tell him ALEX sent you. Growth starts when you take action")]
  [TestCase("Yo man, I caught part of your stream but couldn’t stay long, So I dropped a follow to show some love and support. I can connect you with a pretty big Twitch streamer around 811k+  followers and earns big. He actually helps smaller streamers grow with real audience who might donate, gift and subscribe. If you’re interested, you can add him on Discord: Debby028    Just let him know Cynthia sent you. Thought taking this step could seriously boost your journey as a Streamer")]
  [TestCase("YOO BRO, I’m eager to help you on your journey. I can link you up with a leading Twitch streamer who has 809.6K followers and assists creators in attracting organic viewers, engaged chats, and dedicated followers. If you're truly interested, add him on Discord:   DATTODESTINY  and mention that   Tom sent🫵you.")]
  [TestCase("Add me on discord: heckler80")]
  [TestCase("Add my discord👉 emmanuel_1345")]
  [TestCase("could you add me on Discord  My username is 👉fawaz0160. Appreciate it! 😊")]
  [TestCase("Hello what's up 🥰I love your stream it's very entertaining so I followed you, Got some recommendations that will help you much in improving your stream.. kindly adding up on discord if that's okay:  kings124         Thanks 👍🏿")]
  [TestCase("Hey! You stream really well 👑I’ll  love to become your dedicated fan and support you. Mind adding me on Discord: laycon0801")]
  [TestCase("Hey! Big fan of your content. I’d love to discuss some ideas or just support each other’s growth. Hit me up on Discord: xapar9")]
  [TestCase("Yo bruh \u2764\ufe0f let's sometimes play together and share tips add up on discord \ud83d\udc49 willam0340")]
  [TestCase("Hello mate you stream pretty cool that's why I follow I would love to be a fan of yours if you don't mind kindly add me on Discord: 👉👉lacaster5")]
  [TestCase("Hello, I just recently found your channel and can already tell that your content is great, and I would love to stick with you long term. If you're open to it, I'd be willing to connect with you on Discord. My username is teecash1000")]
  [TestCase("Hey there 👋 You stream pretty cool that’s why I followed you. I will like to make you a friend and be a fan, if you don’t mind Kindly chat me on Discord, my Discord username is tuckzay")]
  [TestCase("Hi! Just wanted to say that I absolutely love your gameplay and content.l'd love to connect better with you on Discord if that's possible. Looking forward to more awesome streams from you! My username is 👉👉👉 edisonpires")]
  [TestCase("What's up Friend, great stream! I'm having a blast watching you stream. Let's move the conversation to Discord, where we can discuss more about streaming in more detail and get to know each other better. See you there! My discord username is 👉john_6029")]
  [TestCase("\"Love your streams! Let's connect on Discord (ice_0147) and build a community! I'd love to support you as a fan & friend!\"")]
  [TestCase("“Hello I  Love your streams, they’re amazing! Let’s connect on Discord (ayome_01) and grow a community. I’d love to support you as a fan and friend!”")]
  public async Task TestKnownStrings(string badString) {
    var rule = new Discord();
    var botProxy = new Mock<ITwitchApiProxy>();
    var chat = new TwitchChatMessage(true, badString, "123", "456");

    // Process the message and assert that we fail the message.
    bool result = await rule.Handle("123", botProxy.Object, chat, _db).ConfigureAwait(false);
    Assert.That(result, Is.False);
  }

  /// <summary>
  ///   Ensure that the rule doesn't fail just because it contains the word discord.
  /// </summary>
  /// <param name="message">The message.</param>
  [Test]
  [TestCase("I've heard of the application discord before and it sounds great")]
  [TestCase("I was talking on my discord the other day")]
  public async Task EnsureNoFalsePositives(string message) {
    var rule = new Discord();
    var botProxy = new Mock<ITwitchApiProxy>();
    var chat = new TwitchChatMessage(true, message, "123", "456");

    // Process the message and assert that we do not fail the message.
    bool result = await rule.Handle("123", botProxy.Object, chat, _db).ConfigureAwait(false);
    Assert.That(result, Is.True);
  }
}