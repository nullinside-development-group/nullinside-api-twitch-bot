using Moq;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.TwitchBot.ChatRules;
using Nullinside.Api.TwitchBot.Model;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

/// <summary>
///   Tests the <see cref="Streamboo" /> class.
/// </summary>
public class StreambooTests : AChatRuleUnitTestBase<Dogehype> {
  /// <summary>
  ///   Tests the strings typed in chats.
  /// </summary>
  /// <param name="badString">The string that should fail.</param>
  [Test]
  [TestCase("streamboo .com ( remove the space )  @anohXiot")]
  [TestCase("streamboo .com ( remove the space )  @C2apJWT9")]
  [TestCase("streamboo .com ( remove the space )  @NpFQHupB")]
  [TestCase("streamboo .com ( remove the space )  @tGYF1O11")]
  public async Task TestKnownStrings(string badString) {
    var rule = new Streamboo();
    var botProxy = new Mock<ITwitchApiProxy>();
    var chat = new TwitchChatMessage(true, badString, "123", "456");

    // Process the message and assert that we fail the message.
    bool result = await rule.Handle("123", botProxy.Object, chat, _db).ConfigureAwait(false);
    Assert.That(result, Is.False);
  }
}