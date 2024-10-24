using System.Diagnostics.CodeAnalysis;

using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   This wrapper exists for unit testing purposes. It's nice passing the <see cref="ChatMessage" /> around only because
///   it promotes discoverability with what chat messages can possess. However, we can't construct a
///   <see cref="ChatMessage" /> easily in the unit tests and thus we need a wrapper that we can mess with that may or may
///   not have a source.
/// </summary>
[ExcludeFromCodeCoverage]
public class TwitchChatMessage {
  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatMessage" /> class.
  /// </summary>
  /// <param name="isFirstMessage">True if this is the first time the user has ever written in this channel.</param>
  /// <param name="message">The message in chat.</param>
  /// <param name="userId">The user id of the sender.</param>
  /// <param name="username">The username of the sender.</param>
  public TwitchChatMessage(bool isFirstMessage, string message, string userId, string username) {
    IsFirstMessage = isFirstMessage;
    Message = message;
    UserId = userId;
    Username = username;
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatMessage" /> class.
  /// </summary>
  /// <param name="message">The chat message from twitch.</param>
  public TwitchChatMessage(ChatMessage message) {
#if DEBUG
    Raw = message;
#endif
    IsFirstMessage = message.IsFirstMessage;
    Message = message.Message;
    UserId = message.UserId;
    Username = message.Username;
  }

  /// <summary>
  ///   True if this is the first time the user has ever written in this channel.
  /// </summary>
  public bool IsFirstMessage { get; set; }

  /// <summary>
  ///   The message in chat.
  /// </summary>
  public string Message { get; set; }

  /// <summary>
  ///   The twitch user id of the chat sender.
  /// </summary>
  public string UserId { get; set; }

  /// <summary>
  ///   The twitch username of the chat sender.
  /// </summary>
  public string Username { get; set; }

#if DEBUG
  /// <summary>
  ///   FOR DEBUGGING PURPOSES ONLY!
  /// </summary>
  public ChatMessage? Raw { get; }
#endif
}