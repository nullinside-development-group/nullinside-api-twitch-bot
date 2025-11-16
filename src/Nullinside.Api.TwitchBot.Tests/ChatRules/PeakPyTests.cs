using Moq;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.TwitchBot.ChatRules;
using Nullinside.Api.TwitchBot.Model;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

/// <summary>
///   Tests the <see cref="Dogehype" /> class.
/// </summary>
public class PeakPyTests : AChatRuleUnitTestBase<PeakPy> {
  /// <summary>
  ///   Tests the strings typed in chats.
  /// </summary>
  /// <param name="badString">The string that should fail.</param>
  [Test]
  [TestCase("@blackysing Sup, your stream could use some hype PeakPy .com fills it quick (remove the space) @4KB,")]
  [TestCase("@ethuins Yo dude, saw your stream's kinda dead rn PeakPy .com can pump some real eyes in there quick (remove the space) @cMr,")]
  [TestCase("@floridean Sup, your stream could use some hype PeakPy .com can pump some real followers in there quick (remove the space) @sR4n35hG,")]
  [TestCase("@floridean Yo, chat's quiet, PeakPy .com brings real viewers instantly (remove the space) @sN3ZZtgF,")]
  public async Task TestKnownStrings(string badString) {
    var rule = new PeakPy();
    var botProxy = new Mock<ITwitchApiProxy>();
    var chat = new TwitchChatMessage(true, badString, "123", "456");

    // Process the message and assert that we fail the message.
    bool result = await rule.Handle("123", botProxy.Object, chat, _db).ConfigureAwait(false);
    Assert.That(result, Is.False);
  }
}