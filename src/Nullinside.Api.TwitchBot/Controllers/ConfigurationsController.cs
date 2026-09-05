using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Model;

namespace Nullinside.Api.TwitchBot.Controllers;

/// <summary>
///   Manages the configuration of the bot.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ConfigurationsController : ControllerBase {
  /// <summary>
  ///   The nullinside api database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   Initializes a new instance of the <see cref="ConfigurationsController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  public ConfigurationsController(INullinsideContext dbContext) {
    _dbContext = dbContext;
  }

  /// <summary>
  ///   Gets the configuration of the currently authenticated user.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The bot's configuration.</returns>
  [HttpGet("me")]
  public async Task<IActionResult> GetConfig(CancellationToken token) {
    User? user = await this.GetUserEntity(_dbContext, token).ConfigureAwait(false);
    if (null == user) {
      return Unauthorized();
    }

    TwitchUserConfig? config =
      await _dbContext.TwitchUserConfig.FirstOrDefaultAsync(c => c.UserId == user.Id, token).ConfigureAwait(false);
    if (null == config) {
      return Ok(new TwitchUserConfigResponse {
        IsEnabled = true,
        BanKnownBots = true,
        ShowOnHomePage = true
      });
    }

    return Ok(new TwitchUserConfigResponse {
      IsEnabled = config.Enabled,
      BanKnownBots = config.BanKnownBots,
      ShowOnHomePage = config.ShowOnHomePage
    });
  }

  /// <summary>
  ///   Updates the configuration of the currently authenticated user.
  /// </summary>
  /// <param name="configResponse">The configuration to apply for the user.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The updated configuration.</returns>
  [HttpPut("me")]
  public async Task<IActionResult> SetConfig(TwitchUserConfigResponse configResponse, CancellationToken token) {
    int? userId = this.GetUserId();
    if (null == userId) {
      return Unauthorized();
    }

    TwitchUserConfig? configDb = await _dbContext.TwitchUserConfig
      .FirstOrDefaultAsync(c => c.UserId == userId, token)
      .ConfigureAwait(false);
    if (null == configDb) {
      await _dbContext.TwitchUserConfig.AddAsync(new TwitchUserConfig {
        BanKnownBots = configResponse.BanKnownBots,
        Enabled = configResponse.IsEnabled,
        ShowOnHomePage = configResponse.ShowOnHomePage,
        UserId = userId.Value,
        UpdatedOn = DateTime.UtcNow
      }, token).ConfigureAwait(false);
    }
    else {
      configDb.Enabled = configResponse.IsEnabled;
      configDb.BanKnownBots = configResponse.BanKnownBots;
      configDb.ShowOnHomePage = configResponse.ShowOnHomePage;
      configDb.UpdatedOn = DateTime.UtcNow;
    }

    await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    return Ok(configResponse);
  }
}