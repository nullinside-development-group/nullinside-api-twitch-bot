using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.TwitchBot.Model;

/// <summary>
///   Extensions for <see cref="NullinsideContext" /> and its ERMs.
/// </summary>
public static class NullinsideContextExtensions {
  /// <summary>
  ///   Gets a twitch api proxy.
  /// </summary>
  /// <param name="user">The user to configure the proxy as.</param>
  /// <param name="api">The twitch api object currently in use.</param>
  /// <returns>The twitch api.</returns>
  public static void Configure(this ITwitchApiProxy api, User user) {
    api.OAuth = new() {
      AccessToken = user.TwitchToken,
      RefreshToken = user.TwitchRefreshToken,
      ExpiresUtc = user.TwitchTokenExpiration
    };
  }

  /// <summary>
  ///   Gets a twitch api proxy and refreshes its token if necessary.
  /// </summary>
  /// <param name="db">The database.</param>
  /// <param name="user">The user to configure the twitch api as.</param>
  /// <param name="api">The twitch api.</param>
  /// <param name="stoppingToken">The stopping token.</param>
  /// <returns>The twitch api.</returns>
  public static async Task<ITwitchApiProxy?> ConfigureApiAndRefreshToken(this INullinsideContext db, User user,
    ITwitchApiProxy api, CancellationToken stoppingToken = new()) {
    api.Configure(user);

    // Refresh its token if necessary.
    if (!(DateTime.UtcNow + TimeSpan.FromHours(1) > user.TwitchTokenExpiration)) {
      return api;
    }

    if (null == await api.RefreshAccessToken(stoppingToken) || null == api.OAuth) {
      return api;
    }

    await db.UpdateOAuthInDatabase(user.Id, api.OAuth, stoppingToken);
    return api;
  }

  /// <summary>
  ///   Updates the OAuth of a user in the database.
  /// </summary>
  /// <param name="db">The database.</param>
  /// <param name="userId">The user whose OAuth should be updated.</param>
  /// <param name="oAuth">The OAuth information.</param>
  /// <param name="stoppingToken">The stopping token.</param>
  /// <returns>The number of state entries written to the database.</returns>
  public static async Task<int> UpdateOAuthInDatabase(this INullinsideContext db, int userId,
    TwitchAccessToken oAuth, CancellationToken stoppingToken = new()) {
    User? row = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, stoppingToken);
    if (null == row) {
      return -1;
    }

    row.TwitchToken = oAuth.AccessToken;
    row.TwitchRefreshToken = oAuth.RefreshToken;
    row.TwitchTokenExpiration = oAuth.ExpiresUtc;
    return await db.SaveChangesAsync(stoppingToken);
  }

  /// <summary>
  ///   Gets a twitch api proxy for the bot user and refreshes its token if necessary.
  /// </summary>
  /// <param name="db">The database.</param>
  /// <param name="api">The twitch api.</param>
  /// <param name="stoppingToken">The stopping token.</param>
  /// <returns>The twitch api.</returns>
  public static async Task<ITwitchApiProxy?> ConfigureBotApiAndRefreshToken(this INullinsideContext db,
    ITwitchApiProxy api, CancellationToken stoppingToken = new()) {
    // Get the bot user's information.
    User? botUser = await db.Users.AsNoTracking()
      .FirstOrDefaultAsync(u => u.TwitchId == Constants.BotId, stoppingToken);
    if (null == botUser) {
      throw new Exception("No bot user in database");
    }

    return await ConfigureApiAndRefreshToken(db, botUser, api, stoppingToken);
  }

  /// <summary>
  ///   Saves twitch bans to the database.
  /// </summary>
  /// <param name="db">The database.</param>
  /// <param name="channelId">The channel the user is being banned in.</param>
  /// <param name="bannedUsers">The users being banned.</param>
  /// <param name="reason">The reason for the bans.</param>
  /// <param name="stoppingToken">The stopping token.</param>
  public static async Task SaveTwitchBans(this INullinsideContext db, string channelId,
    IEnumerable<(string Id, string Username)> bannedUsers, string reason, CancellationToken stoppingToken = new()) {
    List<string> banUserIds = bannedUsers.Select(b => b.Id).ToList();
    HashSet<string?> existingUsers = db.TwitchUser
      .AsNoTracking()
      .Where(u => null != u.TwitchId && banUserIds.Contains(u.TwitchId))
      .Select(u => u.TwitchId)
      .ToHashSet();

    List<TwitchUser> nonExistantUsers = banUserIds
      .Where(u => !existingUsers.Contains(u))
      .Select(c => new TwitchUser {
        TwitchId = c,
        TwitchUsername = bannedUsers.FirstOrDefault(u => string.Equals(u.Id, c)).Username
      })
      .ToList();

    db.TwitchUser.UpdateRange(nonExistantUsers);
    db.TwitchBan
      .AddRange(banUserIds
        .Select(i => new TwitchBan {
          ChannelId = channelId,
          BannedUserTwitchId = i,
          Reason = reason,
          Timestamp = DateTime.UtcNow
        }));
    await db.SaveChangesAsync(stoppingToken);
  }
}