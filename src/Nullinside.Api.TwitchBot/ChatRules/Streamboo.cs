using System.Text.RegularExpressions;

using Nullinside.Api.Common.Extensions;
using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.TwitchBot.Model;

using TwitchUserConfig = Nullinside.Api.Model.Ddl.TwitchUserConfig;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Handles "Streamboo" spam.
/// </summary>
public class Streamboo : AChatRule {
  /// <inheritdoc />
  public override bool ShouldRun(TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <inheritdoc />
  public override async Task<bool> Handle(string channelId, ITwitchApiProxy botProxy, TwitchChatMessage message,
    INullinsideContext db, CancellationToken stoppingToken = new()) {
    // The number of spaces per message may chance, so normalize that and lowercase it for comparison.
    string normalized = message.Message.NormalizeToAscii().ToLowerInvariant();

    // Message will start with any of these variations.
    if (message.IsFirstMessage && Regex.IsMatch(normalized, @"s+\s*t+\s*r+\s*e+\s*a+\s*m+\s*b+\s*o+\s*o+", RegexOptions.IgnoreCase)) {
      await BanAndLog(channelId, botProxy, new[] { (message.UserId, message.Username) },
        "[Bot] Spam (Streamboo)", db, stoppingToken).ConfigureAwait(false);
      return false;
    }

    return true;
  }
}