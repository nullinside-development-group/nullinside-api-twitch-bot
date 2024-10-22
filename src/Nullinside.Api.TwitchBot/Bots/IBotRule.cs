using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.TwitchBot.Bots;

/// <summary>
///   A rule for banning bots.
/// </summary>
public interface IBotRule {
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
  /// <param name="user">The user.</param>
  /// <param name="config">The user's configuration.</param>
  /// <param name="botProxy">The twitch api authenticated as the bot user.</param>
  /// <param name="db">The database.</param>
  /// <param name="stoppingToken">The cancellation token.</param>
  /// <returns>An asynchronous task.</returns>
  public Task Handle(User user, TwitchUserConfig config, ITwitchApiProxy botProxy,
    INullinsideContext db, CancellationToken stoppingToken = new());
}