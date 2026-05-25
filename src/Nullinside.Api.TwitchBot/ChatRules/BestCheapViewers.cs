using Nullinside.Api.Common.Extensions;
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
  public readonly string[] Expected = [
    "best followers",
    "best viewers on",
    "cheap viewers on",
    "cheap folloewrs on",
    "do you want more viewers and to rank higher on the twitch list? you can visit the website",
    "best viewers",
    "top viewers",
    "cheap viewers",
    "cheapest viewers",
    "buy viewers",
    "want popularity",
    "become popular with",
    "promote twitch channels",
    "boosting channels",
    "streaming into the void",
    "get new real viewers",
    "top viewers",
    "viewers *",
    "viewers smm",
    "specialize in promoting twitch channels",
    "stream viewers",
    "real viewers",
    "viewers stream",
    "ai viewer",
    "ai follower"
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
    string normalized = string.Join(' ', message.Message.NormalizeToAscii().Trim().Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
      .ToLowerInvariant();

    foreach (string expected in Expected) {
      if (normalized.Contains(expected, StringComparison.InvariantCultureIgnoreCase)) {
        (string UserId, string Username)[] users = new[] { (message.UserId, message.Username) };
        await BanAndLog(channelId, botProxy, users, "[Bot] Spam (Best Cheap Viewers)", db, stoppingToken)
          .ConfigureAwait(false);
        return false;
      }
    }

    return true;
  }
}