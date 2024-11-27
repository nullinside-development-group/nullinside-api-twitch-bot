using Moq;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.TwitchBot.ChatRules;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

/// <summary>
///   Tests the <see cref="StreamViewers" /> class.
/// </summary>
public class NezhnaTests : AChatRuleUnitTestBase<Nezhna> {
  /// <summary>
  ///   Tests the strings typed in chats.
  /// </summary>
  /// <param name="badString">The string that should fail.</param>
  [Test]
  [TestCase("Visit nezhna .com and watch your channel grow today!  @gG6SC5d3")]
  [TestCase("Visit nezhna dot com to boost your viewers and climb the Twitch rankings. Join thousands of successful streamers now!  @0tlVpgrw")]
  [TestCase("Visit nezhna dot com com to boost your viewers and climb the Twitch rankings. Join thousands of successful streamers now!  @7xgkq3EK")]
  public async Task TestKnownStrings(string badString) {
    var rule = new Nezhna();
    var botProxy = new Mock<ITwitchApiProxy>();
    var chat = new TwitchChatMessage(true, badString, "123", "456");

    // Process the message and assert that we fail the message.
    bool result = await rule.Handle("123", botProxy.Object, chat, _db);
    Assert.That(result, Is.False);
  }
}