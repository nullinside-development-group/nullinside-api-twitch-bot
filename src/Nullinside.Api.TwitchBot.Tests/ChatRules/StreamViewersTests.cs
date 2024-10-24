using Moq;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.TwitchBot.ChatRules;

namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

/// <summary>
///   Tests the <see cref="StreamViewers" /> class.
/// </summary>
public class StreamViewersTests : AChatRuleUnitTestBase<StreamViewers> {
  /// <summary>
  ///   Tests the strings typed in chats.
  /// </summary>
  /// <param name="badString">The string that should fail.</param>
  [Test]
  [TestCase("@jellynyeko dо уоu alrеady triеd strеamviewers  оrg? Real viewеrs, fire works! Тhеy arе now giving оut а frее рackagе for streamers оО")]
  [TestCase("@kygaming98 dо уоu аlready tried streаmviewers  оrg? Real viewers, firе works! Thеy arе now giving оut а freе package fоr streamers oО")]
  [TestCase("@hitpointgame2go do уоu alrеаdy tried strеamviewers  оrg? Rеal viewers, firе works! Theу are nоw giving out a freе package fоr strеаmеrs oO")]
  [TestCase("@jellynyeko do yоu аlrеаdу tried streamviеwers  оrg? Rеal viеwеrs, fire works! Тhеу аre nоw giving out а frее рaсkagе fоr strеamers oO")]
  [TestCase("@kirbyplayinggames do уоu аlrеаdy triеd streаmviewers  оrg? Rеal viеwers, fire wоrks! They аre now giving оut a frее package fоr strеаmers oO")]
  [TestCase("@kygaming98 dо you alrеady tried streаmviewers  оrg? Real viewers, firе works! Тheу arе now giving оut a frее расkаgе fоr streаmеrs oO")]
  [TestCase("@subjectbulbasaur do уоu аlready triеd strеаmviewers  оrg? Real viewеrs, fire works! Thеy arе nоw giving out а frеe packаge for strеаmеrs оO")]
  public async Task TestKnownStrings(string badString) {
    var rule = new StreamViewers();
    var botProxy = new Mock<ITwitchApiProxy>();
    var chat = new TwitchChatMessage(true, badString, "123", "456");

    // Process the message and assert that we fail the message.
    bool result = await rule.Handle("123", botProxy.Object, chat, _db);
    Assert.That(result, Is.False);
  }
}