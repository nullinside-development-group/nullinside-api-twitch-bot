using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.TwitchBot.Model;

/// <summary>
///   A response with information about a bot ban from a channel.
/// </summary>
public class TwitchBanResponse {
  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchBanResponse" /> class.
  /// </summary>
  /// <param name="twitchUsername">The username of the account.</param>
  /// <param name="timestamp">The timestamp of the ban.</param>
  /// <param name="chatLogs">The logs of the chat, if available.</param>
  public TwitchBanResponse(string twitchUsername, DateTime timestamp, IEnumerable<TwitchUserChatLogs>? chatLogs) {
    TwitchUsername = twitchUsername;
    Channels = chatLogs?
      .GroupBy(log => new { log.Channel, ChannelId = log.TwitchId })
      .Select(g => new TwitchChannelLogs(
        g.Key.ChannelId ?? string.Empty,
        g.Key.Channel ?? string.Empty,
        g.Select(log => new TwitchChatLog(log.Message ?? string.Empty, log.Timestamp))
      ));
    Timestamp = timestamp;
  }

  /// <summary>
  ///   The twitch user id of the chat sender.
  /// </summary>
  public string? TwitchId { get; set; }

  /// <summary>
  ///   The twitch username of the chat sender.
  /// </summary>
  public string? TwitchUsername { get; set; }

  /// <summary>
  ///   The timestamp of the ban.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  ///   The logs of the chat, if available.
  /// </summary>
  public IEnumerable<TwitchChannelLogs>? Channels { get; set; }

  /// <summary>
  ///   The chat messages in the channels.
  /// </summary>
  public class TwitchChannelLogs {
    /// <summary>
    ///   Initializes a new instance of the <see cref="TwitchChannelLogs" /> class.
    /// </summary>
    /// <param name="channelId">The channel id.</param>
    /// <param name="channel">The channel name.</param>
    /// <param name="messages">The messages.</param>
    public TwitchChannelLogs(string channelId, string channel, IEnumerable<TwitchChatLog>? messages) {
      ChannelId = channelId;
      Channel = channel;
      Messages = messages;
    }

    /// <summary>
    ///   The identifier of the channel from twitch.
    /// </summary>
    public string? ChannelId { get; set; }

    /// <summary>
    ///   The channel name.
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    ///   The logs of the chat, if available.
    /// </summary>
    public IEnumerable<TwitchChatLog>? Messages { get; set; }
  }

  /// <summary>
  ///   Information about a chat log entry.
  /// </summary>
  public class TwitchChatLog {
    /// <summary>
    ///   Initializes a new instance of the <see cref="TwitchChatLog" /> class.
    /// </summary>
    /// <param name="message">The identifier of the channel from twitch.</param>
    /// <param name="timestamp">The twitch chat log message.</param>
    public TwitchChatLog(string message, DateTime timestamp) {
      Message = message;
      Timestamp = timestamp;
    }

    /// <summary>
    ///   The message sent.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    ///   The UTC timestamp when the message was sent.
    /// </summary>
    public DateTime Timestamp { get; set; }
  }
}