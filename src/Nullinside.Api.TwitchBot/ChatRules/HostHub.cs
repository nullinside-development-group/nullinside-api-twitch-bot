using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles "hosthub.vip" spam.
/// </summary>
public class HostHub : AChatRule {
  /// <inheritdoc />
  public override bool ShouldRun(TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <inheritdoc />
  public override async Task<bool> Handle(string channelId, TwitchApiProxy botProxy, ChatMessage message,
    NullinsideContext db, CancellationToken stoppingToken = new()) {
    // The number of spaces per message may change, so normalize that and lowercase it for comparison.
    string normalized = string.Concat(message.Message.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
      .ToLowerInvariant();

    // Message will contain the site name with different domains.
    if (message.IsFirstMessage && normalized.Contains("hosthub.")) {
      await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
        "[Bot] Spam (HostHub)", db, stoppingToken);
      return false;
    }

    return true;
  }
}