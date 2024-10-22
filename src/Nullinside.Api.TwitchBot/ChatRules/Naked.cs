using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles the want to see her naked porn bots.
/// </summary>
public class Naked : AChatRule {
  private const string _spam = "Want to see her naked?";
  private const string _spam2 = "Want to see me naked?";

  /// <inheritdoc />
  public override bool ShouldRun(TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <inheritdoc />
  public override async Task<bool> Handle(string channelId, ITwitchApiProxy botProxy, ChatMessage message,
    INullinsideContext db, CancellationToken stoppingToken = new()) {
    if (message.IsFirstMessage &&
        (message.Message.TrimStart().StartsWith(_spam, StringComparison.InvariantCultureIgnoreCase) ||
         message.Message.TrimStart().StartsWith(_spam2, StringComparison.InvariantCultureIgnoreCase))) {
      await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
        "[Bot] Spam (Naked)", db, stoppingToken);
      return false;
    }

    return true;
  }
}