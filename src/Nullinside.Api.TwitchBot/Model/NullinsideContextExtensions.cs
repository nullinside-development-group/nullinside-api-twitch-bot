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
  /// <returns>The twitch api.</returns>
  public static TwitchApiProxy GetApi(this User user) {
    return new TwitchApiProxy {
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
  /// <param name="stoppingToken">The stopping token.</param>
  /// <returns>The twitch api.</returns>
  public static async Task<TwitchApiProxy?> GetApiAndRefreshToken(this NullinsideContext db, User user,
    CancellationToken stoppingToken = new()) {
    // Get the API
    TwitchApiProxy api = GetApi(user);

    // Refresh its token if necessary.
    if (!(DateTime.UtcNow + TimeSpan.FromHours(1) > user.TwitchTokenExpiration)) {
      return api;
    }

    if (!await api.RefreshTokenAsync(stoppingToken)) {
      return api;
    }

    User? row = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id, stoppingToken);
    if (null == row) {
      return null;
    }

    row.TwitchToken = api.AccessToken;
    row.TwitchRefreshToken = api.RefreshToken;
    row.TwitchTokenExpiration = api.ExpiresUtc;
    await db.SaveChangesAsync(stoppingToken);

    if (Constants.BotId.Equals(user.TwitchId, StringComparison.InvariantCultureIgnoreCase)) {
      TwitchClientProxy.Instance.TwitchUsername = Constants.BotUsername;
      TwitchClientProxy.Instance.TwitchOAuthToken = api.AccessToken;
    }

    return api;
  }

  /// <summary>
  ///   Gets a twitch api proxy for the bot user and refreshes its token if necessary.
  /// </summary>
  /// <param name="db">The database.</param>
  /// <param name="stoppingToken">The stopping token.</param>
  /// <returns>The twitch api.</returns>
  public static async Task<TwitchApiProxy?> GetBotApiAndRefreshToken(this NullinsideContext db,
    CancellationToken stoppingToken = new()) {
    // Get the bot user's information.
    User? botUser = await db.Users.AsNoTracking()
      .FirstOrDefaultAsync(u => u.TwitchId == Constants.BotId, stoppingToken);
    if (null == botUser) {
      throw new Exception("No bot user in database");
    }

    return await GetApiAndRefreshToken(db, botUser, stoppingToken);
  }
}