namespace Nullinside.Api.TwitchBot.Model;

/// <summary>
///   The twitch bot user configuration.
/// </summary>
public class TwitchUserConfigResponse {
  /// <summary>
  ///   True if the bot is enabled to run, false otherwise.
  /// </summary>
  public bool IsEnabled { get; set; }

  /// <summary>
  ///   True if known bots from bot lists should be banned, false otherwise.
  /// </summary>
  public bool BanKnownBots { get; set; }

  /// <summary>
  ///   Do not display the user on the home page when they're live.
  /// </summary>
  public bool ShowOnHomePage { get; set; }
}