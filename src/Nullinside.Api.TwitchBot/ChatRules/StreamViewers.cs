using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.TwitchBot.Model;

using TwitchUserConfig = Nullinside.Api.Model.Ddl.TwitchUserConfig;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles "streamviewers org" spam.
/// </summary>
public class StreamViewers : AChatRule {
  private const int MinThreshold = 70;

  private const string ExpectedSpamMessage =
    "doyoualreadytriedstreamviewersorg?realviewers,fireworks!theyarenowgivingoutafreepackageforstreamersoo";

  /// <inheritdoc />
  public override bool ShouldRun(TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <inheritdoc />
  public override async Task<bool> Handle(string channelId, ITwitchApiProxy botProxy, TwitchChatMessage message,
    INullinsideContext db, CancellationToken stoppingToken = new()) {
    if (!message.IsFirstMessage) {
      return true;
    }

    List<string> parts = message.Message
      .Split(" ")
      .Where(s => !string.IsNullOrWhiteSpace(s))
      .Select(s => s.ToLowerInvariant())
      .ToList();

    if (!parts.Any() || parts[0].Length == 0) {
      return true;
    }

    // It'll start with an @ to the channel owner.
    if (!'@'.Equals(parts[0][0])) {
      return true;
    }

    // Remove the @ part since its the only thing about the message that will very.
    parts = parts[1..];

    // With no spaces the message will be exactly the length of our spam message.
    string noSpaces = string.Concat(parts);
    if (noSpaces.Length != ExpectedSpamMessage.Length) {
      return true;
    }

    // Determine how similar the message is to the spam message.
    int matches = 0;
    for (int i = 0; i < ExpectedSpamMessage.Length; i++) {
      // If it's not an ascii character it might mean they're trying to obfuscate by swapping out letters that look
      // like ascii characters. Like an e with an accent mark instead of an ascii e. They'll look almost the same to
      // the reader but the character will be different. We can skip these and assume it doesn't count as a match.
      if (noSpaces[i] > 122) {
        continue;
      }

      // The ONLY differences should be that a character in the same spot is a non-ascii character. Otherwise, the
      // strings must match. There shouldn't be any letters in order that we aren't expecting provided they're actually
      // ascii characters. If, for some reason, there is a letter in this position that we aren't expecting it means it
      // is not our spam message.
      if (noSpaces[i] != ExpectedSpamMessage[i]) {
        return true;
      }

      // Otherwise, this is a match.
      ++matches;
    }

    // If we had less character matches than our threshold then this wasn't a spam message.
    if (matches < MinThreshold) {
      return true;
    }

    // Otherwise, we just proved that the message:
    // 1. Is exactly the same length as our spam
    // 2. Had the characters we expected in exactly the correct places over 70 times.
    // 3. In places where the characters didn't match, they only didn't match because the message used a non-english keyboard character. It was never because a different letter was in the position.
    // 4. It was probably an @ mention to another user where the @ was the first thing in the message.
    await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
      "[Bot] Spam (StreamViewers)", db, stoppingToken);
    return false;
  }
}