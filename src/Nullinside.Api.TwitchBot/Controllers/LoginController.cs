using log4net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Common.Twitch.Support;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Shared;

namespace Nullinside.Api.TwitchBot.Controllers;

/// <summary>
///   Provides search capabilities through IMDB public database information.
/// </summary>
[ApiController]
[Route("[controller]")]
public class LoginController : ControllerBase {
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
  private readonly ILog _log = LogManager.GetLogger(typeof(LoginController));

  /// <summary>
  ///   Initializes a new instance of the <see cref="LoginController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  /// <param name="configuration">The application's configuration.</param>
  public LoginController(INullinsideContext dbContext, IConfiguration configuration) {
    _dbContext = dbContext;
    _configuration = configuration;
  }

  /// <summary>
  ///   **NOT CALLED BY SITE OR USERS** This endpoint is called by twitch as part of their oauth workflow. It
  ///   redirects users back to the nullinside website.
  /// </summary>
  /// <param name="code">The credentials provided by twitch.</param>
  /// <param name="api">The twitch api.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>
  ///   A redirect to the nullinside website.
  ///   Errors:
  ///   2 = Internal error generating token.
  ///   3 = Code was invalid
  ///   4 = Twitch account has no email
  /// </returns>
  [AllowAnonymous]
  [HttpGet]
  public async Task<IActionResult> TwitchLogin([FromQuery] string code, [FromServices] ITwitchApiProxy api, CancellationToken token) {
    string? siteUrl = _configuration.GetValue<string>("Api:SiteUrl");
    if (null == await api.CreateAccessToken(code, token)) {
      return Redirect($"{siteUrl}/twitch-bot/config?error={TwitchBotLoginErrors.TwitchErrorWithToken}");
    }

    string? email = await api.GetUserEmail(token);
    if (string.IsNullOrWhiteSpace(email)) {
      return Redirect($"{siteUrl}/twitch-bot/config?error={TwitchBotLoginErrors.TwitchAccountHasNoEmail}");
    }

    (string? id, string? username) user = await api.GetUser(token);
    if (string.IsNullOrWhiteSpace(user.username) || string.IsNullOrWhiteSpace(user.id)) {
      return Redirect($"{siteUrl}/twitch-bot/config?error={TwitchBotLoginErrors.InternalError}");
    }

    string? bearerToken = await UserHelpers.GenerateTokenAndSaveToDatabase(_dbContext, email, token, api.OAuth?.AccessToken,
      api.OAuth?.RefreshToken, api.OAuth?.ExpiresUtc, user.username, user.id);
    if (string.IsNullOrWhiteSpace(bearerToken)) {
      return Redirect($"{siteUrl}/twitch-bot/config?error={TwitchBotLoginErrors.InternalError}");
    }

    return Redirect($"{siteUrl}/twitch-bot/config?token={bearerToken}");
  }
}