using System.Security.Claims;
using System.Text.RegularExpressions;

using log4net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Model;

using TwitchLib.Api.Helix.Models.Moderation.GetModerators;

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
  ///   Regex to find username @ mentions in the chat logs.
  /// </summary>
  /// <remarks>@ followed by non-whitespace characters</remarks>
  private readonly Regex usernameMentions = new(@"@\S+", RegexOptions.Compiled);

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
  /// <param name="configResponse">The configuration to apply for the user.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if they are a mod, false otherwise.</returns>
  [HttpPost]
  [Route("config")]
  public async Task<IActionResult> SetConfig(TwitchUserConfigResponse configResponse, CancellationToken token) {
    Claim? userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);
    if (null == userIdClaim) {
      return Unauthorized();
    }

    int userId = int.Parse(userIdClaim.Value);
    TwitchUserConfig? configDb =
      await _dbContext.TwitchUserConfig.FirstOrDefaultAsync(c => c.UserId == userId, token).ConfigureAwait(false);
    if (null == configDb) {
      await _dbContext.TwitchUserConfig.AddAsync(new TwitchUserConfig {
        BanKnownBots = configResponse.BanKnownBots,
        Enabled = configResponse.IsEnabled,
        ShowOnHomePage = configResponse.ShowOnHomePage,
        UserId = userId,
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

  /// <summary>
  ///   Retrieves all currently live individuals on twitch.
  /// </summary>
  [AllowAnonymous]
  [HttpGet("live")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<ObjectResult> GetAllLiveBotStreams(CancellationToken token = new()) {
    List<TwitchUserLive> currentlyLive = await _dbContext.TwitchUserLive
      .Include(u => u.User)
      .ToListAsync(token)
      .ConfigureAwait(false);

    return Ok(currentlyLive.Select(u => new TwitchLiveUsersResponse(u)).ToList());
  }

  /// <summary>
  ///   Retrieves the list of recently banned bot accounts.
  /// </summary>
  [AllowAnonymous]
  [HttpGet("bans")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<ObjectResult> GetRecentlyBannedBots(CancellationToken token = new()) {
    var recentBans = await (
        from u in _dbContext.TwitchUser
        join b in _dbContext.TwitchBan
          on u.TwitchId equals b.BannedUserTwitchId
        join c in _dbContext.TwitchUserChatLogs
          on u.TwitchId equals c.TwitchId into chatGroup
        orderby b.Timestamp descending
        select new {
          u.TwitchUsername,
          b.Timestamp,
          ChatLogs = chatGroup.OrderByDescending(c => c.Timestamp).ToList()
        }
      )
      .Take(50)
      .ToListAsync(token)
      .ConfigureAwait(false);

    foreach (var bannedUser in recentBans) {
      foreach (TwitchUserChatLogs? chatLog in bannedUser.ChatLogs) {
        if (string.IsNullOrWhiteSpace(chatLog.Message)) {
          continue;
        }

        chatLog.Message = usernameMentions.Replace(chatLog.Message, "****");
      }
    }

    return Ok(recentBans.Select(x => new TwitchRecentBotsResponse(x.TwitchUsername!, x.Timestamp, x.ChatLogs)).ToList());
  }
}