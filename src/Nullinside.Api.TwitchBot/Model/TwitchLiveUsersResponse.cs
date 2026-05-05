using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.TwitchBot.Model;

/// <summary>
///   A response with information about live Twitch streams.
/// </summary>
public class TwitchLiveUsersResponse {
  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchLiveUsersResponse" /> class.
  /// </summary>
  /// <param name="user">The database cache information to pull from.</param>
  public TwitchLiveUsersResponse(TwitchUserLive user) {
    TwitchId = user.User.TwitchId ?? string.Empty;
    ViewerCount = user.ViewerCount;
    GoneLiveTime = user.GoneLiveTime;
    Username = user.User.TwitchUsername;
    StreamTitle = user.StreamTitle;
    GameName = user.GameName;
    ThumbnailUrl = user.ThumbnailUrl;
  }

  /// <summary>
  ///   The unique identifier for the twitch user from twitch.
  /// </summary>
  public string TwitchId { get; set; }

  /// <summary>
  ///   The total view count for the stream.
  /// </summary>
  public int ViewerCount { get; set; }

  /// <summary>
  ///   The time the stream went live.
  /// </summary>
  public DateTime GoneLiveTime { get; set; }

  /// <summary>
  ///   The username of the twitch user.
  /// </summary>
  public string? Username { get; set; }

  /// <summary>
  ///   The title of the stream.
  /// </summary>
  public string? StreamTitle { get; set; }

  /// <summary>
  ///   The name of the game being played on the stream.
  /// </summary>
  public string? GameName { get; set; }

  /// <summary>
  ///   The url of the twitch generated thumbnail for the stream.
  /// </summary>
  public string? ThumbnailUrl { get; set; }
}