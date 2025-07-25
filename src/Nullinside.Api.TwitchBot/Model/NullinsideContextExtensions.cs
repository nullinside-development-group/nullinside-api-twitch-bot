﻿using System.Diagnostics;
using System.Runtime.CompilerServices;

using log4net;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Nullinside.Api.Common;
using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.TwitchBot.Model;

/// <summary>
///   Extensions for <see cref="NullinsideContext" /> and its ERMs.
/// </summary>
public static class NullinsideContextExtensions {
  /// <summary>
  ///   The database lock name to use as a lock in the MySQL database.
  /// </summary>
  private const string BOT_REFRESH_TOKEN_LOCK_NAME = "bot_refresh_token";

  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog LOG = LogManager.GetLogger(typeof(NullinsideContextExtensions));

  /// <summary>
  ///   Gets a twitch api proxy.
  /// </summary>
  /// <param name="user">The user to configure the proxy as.</param>
  /// <param name="api">The twitch api object currently in use.</param>
  /// <returns>The twitch api.</returns>
  public static void Configure(this ITwitchApiProxy api, User user) {
    api.OAuth = new TwitchAccessToken {
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

    // Use the token we have, if it hasn't expired.
    if (!(DateTime.UtcNow + TimeSpan.FromHours(1) > user.TwitchTokenExpiration)) {
      return api;
    }

    // Database locking requires ExecutionStrategy
    IExecutionStrategy strat = db.Database.CreateExecutionStrategy();
    return await strat.ExecuteAsync(async () => {
      bool failed = false;

      // Database locking requires transaction scope
      IDbContextTransaction scope = await db.Database.BeginTransactionAsync(stoppingToken).ConfigureAwait(false);
      await using ConfiguredAsyncDisposable scope1 = scope.ConfigureAwait(false);

      // Perform the database lock
      using var dbLock = new DatabaseLock(db);
      var sw = new Stopwatch();
      sw.Start();
      await dbLock.GetLock(BOT_REFRESH_TOKEN_LOCK_NAME, stoppingToken).ConfigureAwait(false);
      LOG.Info($"bot_refresh_token: {sw.Elapsed}");
      sw.Stop();

      try {
        // Get the user with the database lock acquired.
        User? updatedUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id, stoppingToken).ConfigureAwait(false);
        if (null == updatedUser) {
          return null;
        }

        // Use the token we have, if it hasn't expired.
        if (!(DateTime.UtcNow + TimeSpan.FromHours(1) > updatedUser.TwitchTokenExpiration)) {
          api.Configure(updatedUser);
          return api;
        }

        // Refresh the token with the Twitch API.
        TwitchAccessToken? newToken = await api.RefreshAccessToken(stoppingToken).ConfigureAwait(false);
        if (null == newToken) {
          return null;
        }

        // Update the credentials in the database.
        await db.UpdateOAuthInDatabase(user.Id, newToken, stoppingToken).ConfigureAwait(false);
        return api;
      }
      catch {
        failed = true;
      }
      finally {
        await dbLock.ReleaseLock(BOT_REFRESH_TOKEN_LOCK_NAME, stoppingToken).ConfigureAwait(false);

        if (!failed) {
          scope.Commit();
        }
      }

      return null;
    }).ConfigureAwait(false);
  }

  /// <summary>
  ///   Updates the OAuth of a user in the database.
  /// </summary>
  /// <param name="db">The database.</param>
  /// <param name="userId">The user whose OAuth should be updated.</param>
  /// <param name="oAuth">The OAuth information.</param>
  /// <param name="stoppingToken">The stopping token.</param>
  /// <returns>The number of state entries written to the database.</returns>
  private static async Task<int> UpdateOAuthInDatabase(this INullinsideContext db, int userId,
    TwitchAccessToken oAuth, CancellationToken stoppingToken = new()) {
    User? row = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsBanned, stoppingToken).ConfigureAwait(false);
    if (null == row) {
      return -1;
    }

    row.TwitchToken = oAuth.AccessToken;
    row.TwitchRefreshToken = oAuth.RefreshToken;
    row.TwitchTokenExpiration = oAuth.ExpiresUtc;
    return await db.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
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
      .FirstOrDefaultAsync(u => u.TwitchId == Constants.BOT_ID, stoppingToken).ConfigureAwait(false);
    if (null == botUser) {
      throw new Exception("No bot user in database");
    }

    return await ConfigureApiAndRefreshToken(db, botUser, api, stoppingToken).ConfigureAwait(false);
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
    await db.TwitchUser.UpsertRange(
        bannedUsers.Select(c => new TwitchUser { TwitchId = c.Id, TwitchUsername = c.Username })
          .ToList()
      )
      .On(v => new { v.TwitchId })
      .RunAsync(stoppingToken).ConfigureAwait(false);

    await db.TwitchBan.UpsertRange(
        bannedUsers.Select(i => new TwitchBan {
          ChannelId = channelId,
          BannedUserTwitchId = i.Id,
          Reason = reason,
          Timestamp = DateTime.UtcNow
        }).ToList()
      )
      .On(v => new { v.ChannelId, v.BannedUserTwitchId, v.Timestamp })
      .RunAsync(stoppingToken).ConfigureAwait(false);
  }
}