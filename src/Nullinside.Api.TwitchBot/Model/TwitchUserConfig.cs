namespace Nullinside.Api.TwitchBot.Model;

/// <summary>
///   The twitch bot user configuration.
/// </summary>
public class TwitchUserConfig {
  /// <summary>
  ///   True if the bot is enabled to run, false otherwise.
  /// </summary>
  public bool IsEnabled { get; set; }

  /// <summary>
  ///   True if known bots from bot lists should be banned, false otherwise.
  /// </summary>
  public bool BanKnownBots { get; set; }
}