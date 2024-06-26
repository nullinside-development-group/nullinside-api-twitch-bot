using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model.Ddl;

using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles "dogehype" spam.
/// </summary>
public class Dogehype : IChatRule {
  /// <inheritdoc />
  public bool ShouldRun(TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <inheritdoc />
  public async Task<bool> Handle(string channelId, TwitchApiProxy botProxy, ChatMessage message,
    CancellationToken stoppingToken = new()) {
    // The number of spaces per message may chance, so normalize that and lowercase it for comparison.
    string normalized = string.Concat(message.Message.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
      .ToLowerInvariant();

    // Message will start with any of these variations.
    if (normalized.Contains("dogehype")) {
      await botProxy.BanUsers(channelId, Constants.BotId, new[] { (message.UserId, message.Username) },
        "[Bot] Spam (Dogehype)",
        stoppingToken);
      return false;
    }

    return true;
  }
}