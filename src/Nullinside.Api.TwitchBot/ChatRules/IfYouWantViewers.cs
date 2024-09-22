using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
/// "if you want viewers, go to [link]" scam.
/// </summary>
public class IfYouWantViewers : AChatRule {
  /// <summary>
  ///  The strings that we expect to receive if this is a bot.
  /// </summary>
  public readonly string[] EXPECTED = [
    "if you want more viewers for your stream, go to"
  ];
  
  /// <inheritdoc />
  public override bool ShouldRun(TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <inheritdoc />
  public override async Task<bool> Handle(string channelId, TwitchApiProxy botProxy, ChatMessage message,
    NullinsideContext db, CancellationToken stoppingToken = new()) {
    if (!message.IsFirstMessage) {
      return true;
    }
    
    // The number of spaces per message may chance, so normalize that and lowercase it for comparison.
    string normalized = string.Join(' ', message.Message.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
      .ToLowerInvariant();

    foreach (var expected in EXPECTED) {
      if (normalized.Contains(expected, StringComparison.InvariantCultureIgnoreCase)) {
        await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
          "[Bot] Spam (If You Want Viewers)", db, stoppingToken);
        return false;
      }
    }

    return true;
  }
}