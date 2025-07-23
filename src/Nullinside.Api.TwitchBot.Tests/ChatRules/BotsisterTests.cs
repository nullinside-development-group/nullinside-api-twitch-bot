using Moq;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.TwitchBot.ChatRules;
using Nullinside.Api.TwitchBot.Model;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

/// <summary>
///   Tests the <see cref="Botsister" /> class.
/// </summary>
public class BotsisterTests : AChatRuleUnitTestBase<Botsister> {
  /// <summary>
  ///   Tests the strings typed in chats.
  /// </summary>
  /// <param name="badString">The string that should fail.</param>
  [Test]
  [TestCase("Your chat’s so empty, it’s giving ‘no signal’ vibes. Fix it with botsister .com  @uFi5gGov")]
  public async Task TestKnownStrings(string badString) {
    var rule = new Botsister();
    var botProxy = new Mock<ITwitchApiProxy>();
    var chat = new TwitchChatMessage(true, badString, "123", "456");

    // Process the message and assert that we fail the message.
    bool result = await rule.Handle("123", botProxy.Object, chat, _db).ConfigureAwait(false);
    Assert.That(result, Is.False);
  }
}