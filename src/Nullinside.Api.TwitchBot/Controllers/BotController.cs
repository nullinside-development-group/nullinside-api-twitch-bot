using System.Security.Claims;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common;
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
  ///   The nullinside api database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   Regex to find username @ mentions in the chat logs.
  /// </summary>
  /// <remarks>@ followed by non-whitespace characters</remarks>
  private readonly Regex usernameMentions = new(@"@\S+", RegexOptions.Compiled);

  /// <summary>
  ///   Initializes a new instance of the <see cref="LoginController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  public BotController(INullinsideContext dbContext) {
    _dbContext = dbContext;
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
  [HttpPut]
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
    if (success) {
      return NoContent();
    }

    return Problem(
      title: "Failed to add bot account as moderator",
      detail: "The bot account could not be added as a moderator for the channel.",
      statusCode: StatusCodes.Status500InternalServerError
    );
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
  ///   Updates the configuration.
  /// </summary>
  /// <param name="configResponse">The configuration to apply for the user.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if they are a mod, false otherwise.</returns>
  [HttpPut]
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
  ///   Gets the timestamp of the last time a chat message was received for all live channels.
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
  ///   Retrieves the list of 25 recently banned bot accounts.
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
      .Take(25)
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

    return Ok(recentBans.Select(x => new TwitchRecentBansResponse(x.TwitchUsername!, x.Timestamp, x.ChatLogs)).ToList());
  }

  /// <summary>
  ///   Gets all chat logs from the database.
  /// </summary>
  /// <param name="page">The page number to pull, 1 based.</param>
  /// <param name="pageSize">The page size to pull.</param>
  /// <param name="channel">Optionally, the channel to get chat logs from.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>Twitch chat logs.</returns>
  [Authorize(nameof(UserRoles.ADMIN))]
  [HttpGet("chat/admin")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<ObjectResult> GetAllChatLogs(int page = 1, int pageSize = 100, string? channel = null, CancellationToken token = new()) {
    IQueryable<TwitchUserChatLogs> query = _dbContext.TwitchUserChatLogs.AsQueryable();
    if (null != channel) {
      query = query.Where(c => c.Channel == channel);
    }

    List<TwitchUserChatLogs> logs = await query
      .OrderByDescending(c => c.Timestamp)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync(token)
      .ConfigureAwait(false);

    return Ok(new PagedResponse<TwitchChatLogResponse> {
      Data = logs.Select(l => new TwitchChatLogResponse(l)).ToList(),
      Page = page,
      PageSize = pageSize
    });
  }

  /// <summary>
  ///   Gets time since last chat.
  /// </summary>
  [Authorize(nameof(UserRoles.ADMIN))]
  [HttpGet("chat/timeSince")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<ObjectResult> GetTimeSinceChatForLiveChannels(CancellationToken token = new()) {
    List<TwitchChatTimeSinceResponse> latestMessages = await (
        from live in _dbContext.TwitchUserLive
        join chat in _dbContext.TwitchUserChatLogs
          on live.User.TwitchUsername equals chat.Channel
        group chat by live.User.TwitchUsername
        into chats
        select new TwitchChatTimeSinceResponse(chats.Key, chats.Max(chat => chat.Timestamp))
      )
      .ToListAsync(token)
      .ConfigureAwait(false);

    return Ok(latestMessages);
  }

  /// <summary>
  ///   Gets all banned accounts across all channels with additional information.
  /// </summary>
  [Authorize(nameof(UserRoles.ADMIN))]
  [HttpGet("bans/admin")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<ObjectResult> GetAllBans(int page = 1, int pageSize = 20, CancellationToken token = new()) {
    var recentBans = await (
        from u in _dbContext.TwitchUser
        join b in _dbContext.TwitchBan
          on u.TwitchId equals b.BannedUserTwitchId
        join c in _dbContext.TwitchUserChatLogs
          on u.TwitchId equals c.TwitchId into chatGroup
        orderby b.Timestamp descending
        select new {
          u.TwitchId,
          u.TwitchUsername,
          b.Timestamp,

          Channels = chatGroup
            .GroupBy(x => x.Channel)
            .Select(g => new {
              Channel = g.Key,
              ChannelId = (from channelUser in _dbContext.Users
                where channelUser.TwitchUsername == g.Key
                select channelUser.TwitchId).FirstOrDefault(),

              Messages = g
                .OrderByDescending(x => x.Timestamp)
                .Select(x => new {
                  x.Message,
                  x.Timestamp
                })
                .ToList()
            })
            .ToList()
        }
      )
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync(token)
      .ConfigureAwait(false);

    return Ok(new PagedResponse<TwitchBanResponse> {
      Data = recentBans.Select(ban => new TwitchBanResponse(
        ban.TwitchUsername ?? string.Empty,
        ban.Timestamp,
        ban.Channels?.SelectMany(ch =>
          ch.Messages?.Select(m => new TwitchUserChatLogs {
            Channel = ch.Channel,
            TwitchId = ch.ChannelId,
            Message = m.Message,
            Timestamp = m.Timestamp
          }) ?? Enumerable.Empty<TwitchUserChatLogs>()
        )
      ) {
        TwitchId = ban.TwitchId
      }).ToList(),
      Page = page,
      PageSize = pageSize
    });
  }

  /// <summary>
  ///   Gets the list of accounts with only a few messages that were banned by someone other than us.
  /// </summary>
  [Authorize(nameof(UserRoles.ADMIN))]
  [HttpGet("bans/audit")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<ObjectResult> GetBansNotFromUs(CancellationToken token = new()) {
    List<BansWithMessagesInChat> bans = await _dbContext.BansWithMessagesInChat
      .OrderByDescending(b => b.Timestamp)
      .ToListAsync(token)
      .ConfigureAwait(false);

    return Ok(bans);
  }
}