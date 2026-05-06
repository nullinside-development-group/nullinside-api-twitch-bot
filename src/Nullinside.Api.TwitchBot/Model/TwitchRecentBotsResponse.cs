using Nullinside.Api.Model.Ddl;

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
  /// <param name="chatLogs">The logs of the chat, if available.</param>
  public TwitchRecentBotsResponse(string twitchUsername, DateTime timestamp, IEnumerable<TwitchUserChatLogs>? chatLogs) {
    TwitchUsername = twitchUsername;
    Timestamp = timestamp;
    ChatLogs = chatLogs?.Select(c => new TwitchChatLog(c)).ToList();
  }

  /// <summary>
  ///   The username of the account.
  /// </summary>
  public string TwitchUsername { get; set; }

  /// <summary>
  ///   The timestamp of the ban.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  ///   The logs of the chat, if available.
  /// </summary>
  public IEnumerable<TwitchChatLog>? ChatLogs { get; set; }

  /// <summary>
  ///   Information about a chat log entry.
  /// </summary>
  public class TwitchChatLog {
    /// <summary>
    ///   Initializes a new instance of the <see cref="TwitchChatLog" /> class.
    /// </summary>
    /// <param name="chatLog">The twitch chat log message.</param>
    public TwitchChatLog(TwitchUserChatLogs chatLog) {
      Message = chatLog.Message;
      Timestamp = chatLog.Timestamp;
      TwitchId = chatLog.TwitchId;
      TwitchUsername = chatLog.TwitchUsername;
    }

    /// <summary>
    ///   The message sent.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    ///   The UTC timestamp when the message was sent.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///   The twitch user id of the chat sender.
    /// </summary>
    public string? TwitchId { get; set; }

    /// <summary>
    ///   The twitch username of the chat sender.
    /// </summary>
    public string? TwitchUsername { get; set; }
  }
}