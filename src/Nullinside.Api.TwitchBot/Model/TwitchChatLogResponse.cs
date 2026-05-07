using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.TwitchBot.Model;

/// <summary>
///   A chat log entry.
/// </summary>
public class TwitchChatLogResponse {
  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatLogResponse" /> class.
  /// </summary>
  /// <param name="log">The log message.</param>
  public TwitchChatLogResponse(TwitchUserChatLogs log) {
    Channel = log.Channel;
    TwitchId = log.TwitchId;
    TwitchUsername = log.TwitchUsername;
    Message = log.Message;
    Timestamp = log.Timestamp;
  }

  /// <summary>
  ///   The channel the chat happened in.
  /// </summary>
  public string? Channel { get; set; }

  /// <summary>
  ///   The id of the user that chatted.
  /// </summary>
  /// <remarks>Don't ask me why they made this a string.</remarks>
  public string? TwitchId { get; set; }

  /// <summary>
  ///   The username of the user that chatted.
  /// </summary>
  public string? TwitchUsername { get; set; }

  /// <summary>
  ///   The message sent.
  /// </summary>
  public string? Message { get; set; }

  /// <summary>
  ///   The timestamp of when the message was sent.
  /// </summary>
  public DateTime Timestamp { get; set; }
}