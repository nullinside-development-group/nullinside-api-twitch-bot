using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles "cheap viewers" spam.
/// </summary>
public class BestCheapViewers : AChatRule {
  /// <inheritdoc />
  public override bool ShouldRun(TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <inheritdoc />
  public override async Task<bool> Handle(string channelId, TwitchApiProxy botProxy, ChatMessage message,
    NullinsideContext db, CancellationToken stoppingToken = new()) {
    // The number of spaces per message may chance, so normalize that and lowercase it for comparison.
    string normalized = string.Join(' ', message.Message.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
      .ToLowerInvariant();

    // Message will start with any of these variations.
    if (normalized.StartsWith("cheap viewers on") ||
        normalized.StartsWith("best and cheap viewers on") ||
        normalized.StartsWith("best viewers on")) {
      await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
        "[Bot] Spam (Best Cheap Viewers)", db, stoppingToken);
      return false;
    }

    return true;
  }
}