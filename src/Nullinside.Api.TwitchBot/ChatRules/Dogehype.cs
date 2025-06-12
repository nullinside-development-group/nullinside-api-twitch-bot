using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.TwitchBot.Model;

using TwitchUserConfig = Nullinside.Api.Model.Ddl.TwitchUserConfig;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles "dogehype" spam.
/// </summary>
public class Dogehype : AChatRule {
  /// <inheritdoc />
  public override bool ShouldRun(TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <inheritdoc />
  public override async Task<bool> Handle(string channelId, ITwitchApiProxy botProxy, TwitchChatMessage message,
    INullinsideContext db, CancellationToken stoppingToken = new()) {
    // The number of spaces per message may chance, so normalize that and lowercase it for comparison.
    string normalized = string.Concat(message.Message.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
      .ToLowerInvariant();

    // Message will start with any of these variations.
    if (message.IsFirstMessage && normalized.Contains("dogehype")) {
      await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
        "[Bot] Spam (Dogehype)", db, stoppingToken);
      return false;
    }

    return true;
  }
}