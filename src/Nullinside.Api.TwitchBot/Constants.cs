namespace Nullinside.Api.TwitchBot;

/// <summary>
///   Constants used throughout the application.
/// </summary>
public static class Constants {
  /// <summary>
  ///   The email address associated with the twitch account for the bot.
  /// </summary>
  public const string BOT_EMAIL = "dev.nullinside@gmail.com";

  /// <summary>
  ///   The twitch username for the bot account.
  /// </summary>
  public const string BOT_USERNAME = "nullinside";

  /// <summary>
  ///   The twitch id for the bot account.
  /// </summary>
  public const string BOT_ID = "640082552";
  
  /// <summary>
  /// The amount of time a token is valid for.
  /// </summary>
  public static readonly TimeSpan OAUTH_TOKEN_TIME_LIMIT = TimeSpan.FromHours(1);

  // TODO: This should be dynamic but I need to find a source of "good bots" lists. Might have to cheap out and just do a database table with data entry. Let users of the bot submit suggestions that we approve manually.
  /// <summary>
  ///   The whitelist of bots to not ban.
  /// </summary>
  public static readonly string[] WHITELISTED_BOTS = {
    "soundalerts", "nightbot", "streamlabs",
    "pokemoncommunitygame", "streamelements",
    "moobot", "wizebot", "bad_elbereth", "dixperbro",
    "pretzelrocks", "playwithviewersbot", "blerp",
    "sery_bot", "buttsbot", "songlistbot", "frostytoolsdotcom",
    "kofistreambot", "lumiastream", "botrixoficial", "fossabot",
    "wzbot", "rainmaker", "streamstickers", "tangiabot", "dixperbot",
    "trackerggbot", "creatisbot", "day_walker78"
  };

  /// <summary>
  ///   The minimum time that must elapse between user's getting scanned.
  /// </summary>
  public static readonly TimeSpan MINIMUM_TIME_BETWEEN_SCANS = TimeSpan.FromSeconds(30);

  /// <summary>
  ///   The minimum time that must elapse between user's getting scanned if they are currently live.
  /// </summary>
  public static readonly TimeSpan MINIMUM_TIME_BETWEEN_SCANS_LIVE = TimeSpan.FromSeconds(1);
}