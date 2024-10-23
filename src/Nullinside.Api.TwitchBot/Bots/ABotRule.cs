using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Model;

using TwitchLib.Api.Helix.Models.Moderation.BanUser;

using TwitchUserConfig = Nullinside.Api.Model.Ddl.TwitchUserConfig;

namespace Nullinside.Api.TwitchBot.Bots;

/// <summary>
///   A rule for banning bots.
/// </summary>
public abstract class ABotRule : IBotRule {
  /// <summary>
  ///   Determine if the rule is enabled in the configuration.
  /// </summary>
  /// <param name="config">The user's configuration.</param>
  /// <returns>True if it should run, false otherwise.</returns>
  public abstract bool ShouldRun(TwitchUserConfig config);

  /// <summary>
  ///   Performs the bot banning logic for the rule.
  /// </summary>
  /// <remarks>This should only be called if <see cref="ShouldRun" /> returns true.</remarks>
  /// <param name="user">The user.</param>
  /// <param name="config">The user's configuration.</param>
  /// <param name="botProxy">The twitch api authenticated as the bot user.</param>
  /// <param name="db">The database.</param>
  /// <param name="stoppingToken">The cancellation token.</param>
  /// <returns>An asynchronous task.</returns>
  public abstract Task Handle(User user, TwitchUserConfig config, ITwitchApiProxy botProxy,
    INullinsideContext db, CancellationToken stoppingToken = new());

  /// <summary>
  ///   Handles performing a ban only once, ever, on a user. Any future calls with the same user to be banned in the same
  ///   channel will be skipped.
  /// </summary>
  /// <param name="botProxy">The twitch api authenticated as the bot account.</param>
  /// <param name="db">The database.</param>
  /// <param name="channelId">The channel to ban the user(s) in.</param>
  /// <param name="users">The list of users to ban.</param>
  /// <param name="reason">The reason for the ban.</param>
  /// <param name="stoppingToken">The cancellation token.</param>
  /// <returns>A collection of confirmed banned users.</returns>
  protected virtual async Task<IEnumerable<BannedUser>?> BanOnce(ITwitchApiProxy botProxy, INullinsideContext db,
    string channelId, IEnumerable<(string Id, string Username)> users, string reason,
    CancellationToken stoppingToken = new()) {
    // Get the list of everyone to ban
    List<string> possibleBans = users
      .Select(u => u.Id)
      .ToList();

    // Determine from the database, who has been banned in the past so that we can skip them.
    HashSet<string> existing =
      (from bannedUsers in db.TwitchBan
        where string.Equals(bannedUsers.ChannelId, channelId) &&
              possibleBans.Contains(bannedUsers.BannedUserTwitchId)
        select bannedUsers.BannedUserTwitchId)
      .ToHashSet();

    List<(string Id, string Username)> bansToTry =
      users.Where(possibleBan => !existing.Contains(possibleBan.Id)).ToList();
    if (0 == bansToTry.Count) {
      return [];
    }

    // Perform the ban and get the list of people actually banned
    IEnumerable<BannedUser> confirmedBans =
      await botProxy.BanChannelUsers(channelId, Constants.BotId, bansToTry, reason, stoppingToken);

    await db.SaveTwitchBans(channelId, users, reason, stoppingToken);
    return confirmedBans;
  }
}