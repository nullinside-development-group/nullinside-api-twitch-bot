using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Represents a rule for determining if chat messages come from bots.
/// </summary>
public interface IChatRule {
  /// <summary>
  ///   Determine if the rule is enabled in the configuration.
  /// </summary>
  /// <param name="config">The user's configuration.</param>
  /// <returns>True if it should run, false otherwise.</returns>
  public bool ShouldRun(TwitchUserConfig config);

  /// <summary>
  ///   Performs the bot banning logic for the rule.
  /// </summary>
  /// <remarks>This should only be called if <see cref="ShouldRun" /> returns true.</remarks>
  /// <param name="channelId">The identifier of the channel.</param>
  /// <param name="botProxy">The twitch api authenticated as the bot user.</param>
  /// <param name="message">The chat message.</param>
  /// <param name="db">The database.</param>
  /// <param name="stoppingToken">The cancellation token.</param>
  /// <returns>An asynchronous task.</returns>
  public Task<bool> Handle(string channelId, TwitchApiProxy botProxy, ChatMessage message, NullinsideContext db,
    CancellationToken stoppingToken = new());
}