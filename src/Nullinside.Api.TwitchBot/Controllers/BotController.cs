using System.Security.Claims;

using log4net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Model;

using TwitchLib.Api.Helix.Models.Moderation.GetModerators;

using TwitchUserConfig = Nullinside.Api.TwitchBot.Model.TwitchUserConfig;

namespace Nullinside.Api.TwitchBot.Controllers;

/// <summary>
///   Provides search capabilities through IMDB public database information.
/// </summary>
[ApiController]
[Route("[controller]")]
public class BotController : ControllerBase {
  /// <summary>
  ///   The application's configuration file.
  /// </summary>
  private readonly IConfiguration _configuration;

  /// <summary>
  ///   The nullinside api database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILog _log = LogManager.GetLogger(typeof(BotController));

  /// <summary>
  ///   Initializes a new instance of the <see cref="LoginController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  /// <param name="configuration">The application's configuration.</param>
  public BotController(INullinsideContext dbContext, IConfiguration configuration) {
    _dbContext = dbContext;
    _configuration = configuration;
  }

  /// <summary>
  ///   Checks if the bot account is a moderator.
  /// </summary>
  /// <param name="api">The twitch api.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if they are a mod, false otherwise.</returns>
  [HttpGet]
  [Route("mod")]
  public async Task<IActionResult> IsMod([FromServices] ITwitchApiProxy api, CancellationToken token = new()) {
    Claim? userId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);
    if (null == userId) {
      return Unauthorized();
    }

    User? user = _dbContext.Users.FirstOrDefault(u => u.Id == int.Parse(userId.Value) && !u.IsBanned);
    if (null == user || null == user.TwitchToken || null == user.TwitchRefreshToken ||
        null == user.TwitchTokenExpiration || null == user.TwitchId) {
      return Unauthorized();
    }

    api.Configure(user);
    IEnumerable<Moderator> mods = await api.GetChannelMods(user.TwitchId, token).ConfigureAwait(false);
    return Ok(new {
      isMod = null != mods.FirstOrDefault(m =>
        string.Equals(m.UserId, Constants.BOT_ID, StringComparison.InvariantCultureIgnoreCase))
    });
  }

  /// <summary>
  ///   Mods the bot account.
  /// </summary>
  /// <param name="api">The twitch api.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if they are a mod, false otherwise.</returns>
  [HttpPost]
  [Route("mod")]
  public async Task<IActionResult> ModBotAccount([FromServices] ITwitchApiProxy api, CancellationToken token) {
    Claim? userId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);
    if (null == userId) {
      return Unauthorized();
    }

    User? user = _dbContext.Users.FirstOrDefault(u => u.Id == int.Parse(userId.Value) && !u.IsBanned);
    if (null == user || null == user.TwitchToken || null == user.TwitchRefreshToken ||
        null == user.TwitchTokenExpiration || null == user.TwitchId) {
      return Unauthorized();
    }

    api.Configure(user);
    bool success = await api.AddChannelMod(user.TwitchId, Constants.BOT_ID, token).ConfigureAwait(false);
    return Ok(success);
  }

  /// <summary>
  ///   Checks if the bot account is a moderator.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if they are a mod, false otherwise.</returns>
  [HttpGet]
  [Route("config")]
  public async Task<IActionResult> GetConfig(CancellationToken token) {
    Claim? userId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);
    if (null == userId) {
      return Unauthorized();
    }

    User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(userId.Value) && !u.IsBanned, token).ConfigureAwait(false);
    if (null == user) {
      return Unauthorized();
    }

    Api.Model.Ddl.TwitchUserConfig? config =
      await _dbContext.TwitchUserConfig.FirstOrDefaultAsync(c => c.UserId == user.Id, token).ConfigureAwait(false);
    if (null == config) {
      return Ok(new TwitchUserConfig {
        IsEnabled = true,
        BanKnownBots = true
      });
    }

    return Ok(new TwitchUserConfig {
      IsEnabled = config.Enabled,
      BanKnownBots = config.BanKnownBots
    });
  }

  /// <summary>
  ///   Gets the timestamp of the last time a chat message was received.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The timestamp of the last message received.</returns>
  [AllowAnonymous]
  [HttpGet]
  [Route("chat/timestamp")]
  public async Task<IActionResult> GetLastChatTimestamp(CancellationToken token) {
    TwitchUserChatLogs? message =
      await _dbContext.TwitchUserChatLogs.OrderByDescending(c => c.Timestamp).FirstOrDefaultAsync(token).ConfigureAwait(false);
    if (null == message) {
      return StatusCode(500);
    }

    return Ok(message.Timestamp);
  }

  /// <summary>
  ///   Updates the configuration.
  /// </summary>
  /// <param name="config">The configuration to apply for the user.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if they are a mod, false otherwise.</returns>
  [HttpPost]
  [Route("config")]
  public async Task<IActionResult> SetConfig(TwitchUserConfig config, CancellationToken token) {
    Claim? userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);
    if (null == userIdClaim) {
      return Unauthorized();
    }

    int userId = int.Parse(userIdClaim.Value);
    Api.Model.Ddl.TwitchUserConfig? configDb =
      await _dbContext.TwitchUserConfig.FirstOrDefaultAsync(c => c.UserId == userId, token).ConfigureAwait(false);
    if (null == configDb) {
      await _dbContext.TwitchUserConfig.AddAsync(new Api.Model.Ddl.TwitchUserConfig {
        BanKnownBots = config.BanKnownBots,
        Enabled = config.IsEnabled,
        UserId = userId,
        UpdatedOn = DateTime.UtcNow
      }, token).ConfigureAwait(false);
    }
    else {
      configDb.Enabled = config.IsEnabled;
      configDb.BanKnownBots = config.BanKnownBots;
      configDb.UpdatedOn = DateTime.UtcNow;
    }

    await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    return Ok(config);
  }
}