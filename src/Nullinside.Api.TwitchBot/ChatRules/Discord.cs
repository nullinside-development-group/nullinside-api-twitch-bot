using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.TwitchBot.Model;

using TwitchLib.Client.Models;

using TwitchUserConfig = Nullinside.Api.Model.Ddl.TwitchUserConfig;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles the add me on discord bots.
/// </summary>
public class Discord : AChatRule {
  private readonly string[] KNOWN_PHRASES = [
    "add me on discord",
    "my username is",
    "my discord username is",
    "connect on discord"
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

    if (!normalized.Contains("discord", StringComparison.InvariantCultureIgnoreCase)) {
      return true;
    }

    foreach (var phrase in KNOWN_PHRASES) {
      if (normalized.Contains(phrase, StringComparison.InvariantCultureIgnoreCase)) {
        await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
          "[Bot] Spam (Discord Scammers)", db, stoppingToken);
        return false;
      }
    }

    return true;
  }
}