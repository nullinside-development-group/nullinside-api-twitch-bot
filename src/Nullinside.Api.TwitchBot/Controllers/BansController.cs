using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Model;

namespace Nullinside.Api.TwitchBot.Controllers;

/// <summary>
///   Manages ban resources.
/// </summary>
[ApiController]
[Route("[controller]")]
public class BansController : ControllerBase {
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
  ///   Initializes a new instance of the <see cref="BansController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  public BansController(INullinsideContext dbContext) {
    _dbContext = dbContext;
  }

  /// <summary>
  ///   Gets most recently banned bot accounts.
  /// </summary>
  /// <param name="limit">The number of bots to return.</param>
  /// <param name="token">The cancellation token.</param>
  [AllowAnonymous]
  [HttpGet]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<ObjectResult> GetRecentlyBannedBots(int limit = 25, CancellationToken token = new()) {
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
      .Take(limit)
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
  ///   Gets the bans, not by the bot, that had very few messages.
  /// </summary>
  [Authorize(nameof(UserRoles.ADMIN))]
  [HttpGet("external")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<ObjectResult> GetBansNotFromUs(CancellationToken token = new()) {
    List<BansWithMessagesInChat> bans = await _dbContext.BansWithMessagesInChat
      .OrderByDescending(b => b.Timestamp)
      .ToListAsync(token)
      .ConfigureAwait(false);

    return Ok(bans);
  }
}