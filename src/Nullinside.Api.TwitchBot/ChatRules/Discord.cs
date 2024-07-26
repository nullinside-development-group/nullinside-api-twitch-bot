using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles the add me on discord bots.
/// </summary>
public class Discord : AChatRule {
  private const string _spam = "I would like to be on of your fans if you don't mind kindly add me up on discord";

  /// <inheritdoc />
  public override bool ShouldRun(TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <inheritdoc />
  public override async Task<bool> Handle(string channelId, TwitchApiProxy botProxy, ChatMessage message,
    NullinsideContext db, CancellationToken stoppingToken = new()) {
    if (message.IsFirstMessage &&
        message.Message.Contains(_spam, StringComparison.InvariantCultureIgnoreCase)) {
      await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
        "[Bot] Spam (Discord Freaks)", db, stoppingToken);
      return false;
    }

    return true;
  }
}