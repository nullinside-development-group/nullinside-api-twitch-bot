namespace Nullinside.Api.TwitchBot.Model;

/// <summary>
///   The amount of time since a chat was received for a channel.
/// </summary>
public class TwitchChatTimeSinceResponse {
  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatTimeSinceResponse" /> class.
  /// </summary>
  /// <param name="channel">The channel the message was in.</param>
  /// <param name="latestMessage">The timestamp of the latest message.</param>
  public TwitchChatTimeSinceResponse(string channel, DateTime latestMessage) {
    Channel = channel;
    LatestMessage = latestMessage;
  }

  /// <summary>
  ///   The channel the message was in.
  /// </summary>
  public string Channel { get; set; }

  /// <summary>
  ///   The timestamp of the latest message.
  /// </summary>
  public DateTime LatestMessage { get; set; }
}