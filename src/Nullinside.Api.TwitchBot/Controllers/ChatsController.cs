using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Model;

namespace Nullinside.Api.TwitchBot.Controllers;

/// <summary>
///   Manages twitch chat message resources.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ChatsController : ControllerBase {
  /// <summary>
  ///   The nullinside api database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   Initializes a new instance of the <see cref="LoginController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  public ChatsController(INullinsideContext dbContext) {
    _dbContext = dbContext;
  }

  /// <summary>
  ///   Gets the current status of all chat resources containing a timestamp for the last chat message received from any
  ///   chat the bot monitors.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The timestamp of the last message received.</returns>
  [AllowAnonymous]
  [HttpGet]
  [Route("status")]
  public async Task<IActionResult> GetLastChatTimestamp(CancellationToken token) {
    TwitchUserChatLogs? message = await _dbContext.TwitchUserChatLogs.OrderByDescending(c => c.Timestamp).FirstOrDefaultAsync(token).ConfigureAwait(false);
    if (null == message) {
      return StatusCode(500);
    }

    return Ok(message.Timestamp);
  }

  /// <summary>
  ///   Gets chat messages for the currently authenticated user's stream.
  /// </summary>
  /// <param name="page">The page number to pull, 1 based.</param>
  /// <param name="pageSize">The page size to pull.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The chat logs.</returns>
  [HttpGet("me")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<ObjectResult> GetChatLogs(int page = 1, int pageSize = 100, CancellationToken token = new()) {
    User? user = await this.GetUserEntity(_dbContext, token).ConfigureAwait(false);
    if (null == user) {
      return Unauthorized(false);
    }

    List<TwitchUserChatLogs> logs = await _dbContext.TwitchUserChatLogs
      .Where(x => x.Channel == user.TwitchUsername)
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
  ///   Gets all chat messages, optionally from a specific channel.
  /// </summary>
  /// <param name="channel">Optionally the name of the channel to retrieve logs from, all otherwise.</param>
  /// <param name="page">The page number to retrieve.</param>
  /// <param name="pageSize">The number of records per page.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The chat logs.</returns>
  [Authorize(nameof(UserRoles.ADMIN))]
  [HttpGet]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<ObjectResult> GetAllChatLogs(string? channel = null, int page = 1, int pageSize = 100, CancellationToken token = new()) {
    IQueryable<TwitchUserChatLogs> query = _dbContext.TwitchUserChatLogs.AsQueryable();
    if (!string.IsNullOrWhiteSpace(channel)) {
      query = query.Where(x => x.Channel == channel);
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
  ///   Gets the activity of all chat resources which includes the timestamp of the last message received.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The activity of all chats.</returns>
  [Authorize(nameof(UserRoles.ADMIN))]
  [HttpGet("activity")]
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
}