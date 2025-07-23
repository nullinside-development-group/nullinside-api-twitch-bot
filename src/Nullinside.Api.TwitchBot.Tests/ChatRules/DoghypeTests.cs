using Moq;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.TwitchBot.ChatRules;
using Nullinside.Api.TwitchBot.Model;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

/// <summary>
///   Tests the <see cref="Dogehype" /> class.
/// </summary>
public class DogehypeTests : AChatRuleUnitTestBase<Dogehype> {
  /// <summary>
  ///   Tests the strings typed in chats.
  /// </summary>
  /// <param name="badString">The string that should fail.</param>
  [Test]
  [TestCase("Visit dogehype dot com today and effortlessly boost your Twitch rankings!  @opAzPMVt")]
  [TestCase("Visit dogehype dot com and watch your channel grow today!  @5MGxTnYl")]
  [TestCase("Visit dogehype dot com today and climb the Twitch rankings with ease! Whether you're just starting out or looking to take your stream to the next level, DogeHype has the tools you need to succeed.  @gqznceDC")]
  [TestCase("Visit dogehype .biz com today and climb the Twitch rankings with ease! Whether you're just starting out or looking to take your stream to the next level, DogeHype has the tools you need to succeed.  @Axxq7ntz")]
  public async Task TestKnownStrings(string badString) {
    var rule = new Dogehype();
    var botProxy = new Mock<ITwitchApiProxy>();
    var chat = new TwitchChatMessage(true, badString, "123", "456");

    // Process the message and assert that we fail the message.
    bool result = await rule.Handle("123", botProxy.Object, chat, _db).ConfigureAwait(false);
    Assert.That(result, Is.False);
  }
}