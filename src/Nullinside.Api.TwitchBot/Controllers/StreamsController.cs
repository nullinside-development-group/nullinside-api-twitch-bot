using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Model;

namespace Nullinside.Api.TwitchBot.Controllers;

/// <summary>
///   Manages twitch stream resources.
/// </summary>
[ApiController]
[Route("[controller]")]
public class StreamsController : ControllerBase {
  /// <summary>
  ///   The nullinside api database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   Initializes a new instance of the <see cref="StreamsController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  public StreamsController(INullinsideContext dbContext) {
    _dbContext = dbContext;
  }

  /// <summary>
  ///   Gets all streams where the bot is enabled, the bot is a moderator, and the stream is live.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The live stream resources using the bot.</returns>
  [AllowAnonymous]
  [HttpGet]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<ObjectResult> GetAllLiveBotStreams(CancellationToken token = new()) {
    List<TwitchUserLive> currentlyLive = await _dbContext.TwitchUserLive
      .Include(u => u.User)
      .ToListAsync(token)
      .ConfigureAwait(false);

    return Ok(currentlyLive.Select(u => new TwitchLiveUsersResponse(u)).ToList());
  }
}