using Microsoft.AspNetCore.Mvc;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Model;

using TwitchLib.Api.Helix.Models.Moderation.GetModerators;

namespace Nullinside.Api.TwitchBot.Controllers;

/// <summary>
///   Manages twitch moderation-related resources.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ModeratorsController : ControllerBase {
  /// <summary>
  ///   The nullinside api database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   Initializes a new instance of the <see cref="ModeratorsController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  public ModeratorsController(INullinsideContext dbContext) {
    _dbContext = dbContext;
  }

  /// <summary>
  ///   Gets the moderation status of the bot for the currently authenticated user.
  /// </summary>
  /// <param name="api">The twitch api.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The role of the bot either moderator or viewer.</returns>
  [HttpGet]
  [Route("bot")]
  public async Task<IActionResult> IsMod([FromServices] ITwitchApiProxy api, CancellationToken token = new()) {
    User? user = await this.GetUserEntity(_dbContext, token).ConfigureAwait(false);
    if (null == user || null == user.TwitchToken || null == user.TwitchRefreshToken ||
        null == user.TwitchTokenExpiration || null == user.TwitchId) {
      return Unauthorized();
    }

    api.Configure(user);
    IEnumerable<Moderator> mods = await api.GetChannelMods(user.TwitchId, token).ConfigureAwait(false);
    bool botIsMod = null != mods.FirstOrDefault(m => string.Equals(m.UserId, Constants.BOT_ID, StringComparison.InvariantCultureIgnoreCase));
    return Ok(new {
      twitchUserId = Constants.BOT_ID,
      twitchUsername = Constants.BOT_USERNAME,
      role = botIsMod ? "moderator" : "viewer"
    });
  }

  /// <summary>
  ///   Updates the moderation status of the bot to moderator for the currently authenticated user.
  /// </summary>
  /// <param name="api">The twitch api.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>No content if successful.</returns>
  [HttpPut]
  [Route("bot")]
  public async Task<IActionResult> ModBotAccount([FromServices] ITwitchApiProxy api, CancellationToken token) {
    User? user = await this.GetUserEntity(_dbContext, token).ConfigureAwait(false);
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
}