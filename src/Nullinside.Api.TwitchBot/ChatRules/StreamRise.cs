using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model.Ddl;

using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles "streamrise" spam.
/// </summary>
public class StreamRise : IChatRule {
  private const string _spam = "Hello, sorry for bothering you. I want to offer promotion of your channel, " +
                               "viewers, followers, views, chat bots, etc...The price is lower than any competitor, " +
                               "the quality is guaranteed to be the best.   Flexible and convenient order management " +
                               "panel, chat panel, everything is in your hands, a huge number of custom settings. Go " +
                               "to streamrise";

  /// <inheritdoc />
  public bool ShouldRun(TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <inheritdoc />
  public async Task<bool> Handle(string channelId, TwitchApiProxy botProxy, ChatMessage message,
    CancellationToken stoppingToken = new()) {
    if (_spam.Equals(message.Message, StringComparison.InvariantCultureIgnoreCase)) {
      await botProxy.BanUsers(channelId, Constants.BotId, new[] { (message.UserId, message.Username) },
        "[Bot] Spam (StreamRise)",
        stoppingToken);
      return false;
    }

    return true;
  }
}