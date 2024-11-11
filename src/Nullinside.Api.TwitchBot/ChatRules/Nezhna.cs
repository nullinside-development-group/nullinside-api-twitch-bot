using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles the "nezhna dot com" bots.
/// </summary>
public class Nezhna : AChatRule {
  private const string SPAM = "Visit nezhna dot com com to boost your viewers and climb the Twitch rankings. Join thousands of successful streamers now!";

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

    if (normalized.StartsWith(SPAM, StringComparison.InvariantCultureIgnoreCase)) {
      await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
        "[Bot] Spam (Nezhna)", db, stoppingToken);
      return false;
    }
    
    return true;
  }
}