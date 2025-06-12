using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.TwitchBot.Model;

using TwitchUserConfig = Nullinside.Api.Model.Ddl.TwitchUserConfig;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles "cheap viewers" spam.
/// </summary>
public class BestCheapViewers : AChatRule {
  /// <summary>
  ///   The strings that we expect to receive if this is a bot.
  /// </summary>
  public readonly string[] EXPECTED = [
    "best viewers on",
    "cheap viewers on",
    "cheap folloewrs on",
    "do you want more viewers and to rank higher on the twitch list? you can visit the website"
  ];

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

    // The number of spaces per message may chance, so normalize that and lowercase it for comparison.
    string normalized = string.Join(' ', message.Message.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
      .ToLowerInvariant();

    // Messages will be one of two variations with random special characters mixed in. Some of those special characters
    // will be accent marks. When we receive an accent mark it'll take the position of a real character, hence why we
    // need an offset applied only to the incoming string.
    foreach (string expected in EXPECTED) {
      if (normalized.Length > expected.Length) {
        int matches = 0;
        int offset = 0;
        for (int i = 0; i < expected.Length; i++) {
          // If this is a normal character it should be in the correct position.
          if (i + offset < normalized.Length && normalized[i + offset] == expected[i]) {
            ++matches;
          }
          // If this is an accent mark then the next character should match and the whole string we're evalutating
          // will be off by 1 more position.
          else if (i + offset + 1 < normalized.Length && normalized[i + offset + 1] == expected[i]) {
            ++matches;
            ++offset;
          }
        }

        // If everything matches except 3 characters, take it. We will assume the 3 characters are "special" characters
        // used to confuse us.
        if (matches > expected.Length - 3) {
          await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
            "[Bot] Spam (Best Cheap Viewers)", db, stoppingToken);
          return false;
        }
      }
    }

    return true;
  }
}