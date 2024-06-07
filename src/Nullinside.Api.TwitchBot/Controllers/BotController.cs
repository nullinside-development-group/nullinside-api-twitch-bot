using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

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
  private readonly NullinsideContext _dbContext;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILogger<BotController> _logger;

  /// <summary>
  ///   Initializes a new instance of the <see cref="LoginController" /> class.
  /// </summary>
  /// <param name="logger">The logger.</param>
  /// <param name="dbContext">The nullinside database.</param>
  /// <param name="configuration">The application's configuration.</param>
  public BotController(ILogger<BotController> logger, NullinsideContext dbContext, IConfiguration configuration) {
    _logger = logger;
    _dbContext = dbContext;
    _configuration = configuration;
  }

  /// <summary>
  ///   Checks if the bot account is a moderator.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if they are a mod, false otherwise.</returns>
  [HttpGet]
  [Route("mod")]
  public async Task<IActionResult> IsMod(CancellationToken token) {
    Claim? userId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);
    if (null == userId) {
      return Unauthorized();
    }

    User? user = _dbContext.Users.FirstOrDefault(u => u.Id == int.Parse(userId.Value));
    if (null == user || null == user.TwitchToken || null == user.TwitchRefreshToken ||
        null == user.TwitchTokenExpiration || null == user.TwitchId) {
      return Unauthorized();
    }

    var api = new TwitchApiProxy(user.TwitchToken, user.TwitchRefreshToken, user.TwitchTokenExpiration.Value);
    IEnumerable<Moderator> mods = await api.GetMods(user.TwitchId, token);
    return Ok(new {
      isMod = null != mods.FirstOrDefault(m =>
        string.Equals(m.UserId, Constants.BotId, StringComparison.InvariantCultureIgnoreCase))
    });
  }

  /// <summary>
  ///   Mods the bot account.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if they are a mod, false otherwise.</returns>
  [HttpPost]
  [Route("mod")]
  public async Task<IActionResult> ModBotAccount(CancellationToken token) {
    Claim? userId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);
    if (null == userId) {
      return Unauthorized();
    }

    User? user = _dbContext.Users.FirstOrDefault(u => u.Id == int.Parse(userId.Value));
    if (null == user || null == user.TwitchToken || null == user.TwitchRefreshToken ||
        null == user.TwitchTokenExpiration || null == user.TwitchId) {
      return Unauthorized();
    }

    var api = new TwitchApiProxy(user.TwitchToken, user.TwitchRefreshToken, user.TwitchTokenExpiration.Value);
    bool success = await api.ModAccount(user.TwitchId, Constants.BotId, token);
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

    User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(userId.Value), token);
    if (null == user) {
      return Unauthorized();
    }

    Api.Model.Ddl.TwitchUserConfig? config =
      await _dbContext.TwitchUserConfig.FirstOrDefaultAsync(c => c.UserId == user.Id, token);
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
      await _dbContext.TwitchUserConfig.FirstOrDefaultAsync(c => c.UserId == userId, token);
    if (null == configDb) {
      await _dbContext.TwitchUserConfig.AddAsync(new Api.Model.Ddl.TwitchUserConfig {
        BanKnownBots = config.BanKnownBots,
        Enabled = config.IsEnabled,
        UserId = userId,
        UpdatedOn = DateTime.UtcNow
      }, token);
    }
    else {
      configDb.Enabled = config.IsEnabled;
      configDb.BanKnownBots = config.BanKnownBots;
      configDb.UpdatedOn = DateTime.UtcNow;
    }

    await _dbContext.SaveChangesAsync(token);
    return Ok(config);
  }
}