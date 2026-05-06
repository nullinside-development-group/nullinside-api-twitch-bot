namespace Nullinside.Api.TwitchBot.Model;

/// <summary>
///   A response with information about live Twitch streams.
/// </summary>
public class TwitchRecentBotsResponse {
  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchRecentBotsResponse" /> class.
  /// </summary>
  /// <param name="twitchUsername">The username of the account.</param>
  /// <param name="timestamp">The timestamp of the ban.</param>
  public TwitchRecentBotsResponse(string twitchUsername, DateTime timestamp) {
    TwitchUsername = twitchUsername;
    Timestamp = timestamp;
  }

  /// <summary>
  ///   The username of the account.
  /// </summary>
  public string TwitchUsername { get; set; }

  /// <summary>
  ///   The timestamp of the ban.
  /// </summary>
  public DateTime Timestamp { get; set; }
}