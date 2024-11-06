using log4net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nullinside.Api.TwitchBot.Controllers;

/// <summary>
/// testing
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("[controller]")]
public class TwitchWebHookController : ControllerBase {
  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILog _log = LogManager.GetLogger(typeof(TwitchWebHookController));
  
  /// <summary>
  /// testing
  /// </summary>
  /// <param name="stuff"></param>
  /// <param name="token"></param>
  /// <returns></returns>
  [HttpPost]
  [Route("chat")]
  public async Task<IActionResult> TwitchChatMessageCallback(CancellationToken token) {
    using StreamReader stream = new StreamReader(this.HttpContext.Request.Body);
    string stuff = await stream.ReadToEndAsync(token);
    _log.Info($"TwitchChatMessageCallback: {stuff}");
    return Ok(true);
  }
}