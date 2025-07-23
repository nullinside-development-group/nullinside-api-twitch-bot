using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.TwitchBot.Model;

using TwitchUserConfig = Nullinside.Api.Model.Ddl.TwitchUserConfig;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles the want to see her naked porn bots.
/// </summary>
public class Naked : AChatRule {
  private const string SPAM = "Want to see her naked?";
  private const string SPAM2 = "Want to see me naked?";

  /// <inheritdoc />
  public override bool ShouldRun(TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <inheritdoc />
  public override async Task<bool> Handle(string channelId, ITwitchApiProxy botProxy, TwitchChatMessage message,
    INullinsideContext db, CancellationToken stoppingToken = new()) {
    if (message.IsFirstMessage &&
        (message.Message.TrimStart().StartsWith(SPAM, StringComparison.InvariantCultureIgnoreCase) ||
         message.Message.TrimStart().StartsWith(SPAM2, StringComparison.InvariantCultureIgnoreCase))) {
      await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
        "[Bot] Spam (Naked)", db, stoppingToken).ConfigureAwait(false);
      return false;
    }

    return true;
  }
}