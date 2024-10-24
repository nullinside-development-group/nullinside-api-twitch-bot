using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.TwitchBot.Model;

using TwitchLib.Client.Models;

using TwitchUserConfig = Nullinside.Api.Model.Ddl.TwitchUserConfig;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Represents the basis for a rule for determining if chat messages come from bots.
/// </summary>
public abstract class AChatRule : IChatRule {
  /// <inheritdoc />
  public abstract bool ShouldRun(TwitchUserConfig config);

  /// <inheritdoc />
  public abstract Task<bool> Handle(string channelId, ITwitchApiProxy botProxy, TwitchChatMessage message,
    INullinsideContext db, CancellationToken stoppingToken = new());

  /// <summary>
  ///   Bans a user and logs the ban attempt to the database.
  /// </summary>
  /// <param name="channelId">The identifier of the channel.</param>
  /// <param name="botProxy">The twitch api authenticated as the bot user.</param>
  /// <param name="users">The users to ban by their twitch id and twitch username.</param>
  /// <param name="reason">The ban reason.</param>
  /// <param name="db">The database.</param>
  /// <param name="stoppingToken">The cancellation token.</param>
  public async Task BanAndLog(string channelId, ITwitchApiProxy botProxy,
    IEnumerable<(string Id, string Username)> users, string reason, INullinsideContext db,
    CancellationToken stoppingToken = new()) {
    await botProxy.BanChannelUsers(channelId, Constants.BotId, users, reason, stoppingToken);
    await db.SaveTwitchBans(channelId, users, reason, stoppingToken);
  }
}