using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

using Newtonsoft.Json;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Model;

using TwitchLib.Api.Helix.Models.Chat.GetChatters;

using TwitchUserConfig = Nullinside.Api.Model.Ddl.TwitchUserConfig;

namespace Nullinside.Api.TwitchBot.Bots;

/// <summary>
///   Searches for and bans known bots when they enter the chat.
/// </summary>
public class BanKnownBots : ABotRule {
  /// <summary>
  ///   Handles refreshing the <see cref="KnownBotListUsername" /> collection.
  /// </summary>
  private static Task _ = Task.Run(async () => {
    while (true) {
      Task<ImmutableHashSet<string>?> twitchInsights = GetTwitchInsightsBots();
      Task<ImmutableHashSet<string>?> commanderRoot = GetCommanderRootBots();
      await Task.WhenAll(twitchInsights, commanderRoot);
      if (null != twitchInsights.Result) {
        KnownBotListUsername = twitchInsights.Result;
      }

      if (null != commanderRoot.Result) {
        KnownBotListUserId = commanderRoot.Result;
      }

      // The list is enormous, force garbage collection.
      GC.Collect();
      GC.WaitForPendingFinalizers();
      await Task.Delay(TimeSpan.FromMinutes(10));
    }
  });

  /// <summary>
  ///   The cache of known bots.
  /// </summary>
  public static ImmutableHashSet<string>? KnownBotListUsername { get; set; }

  /// <summary>
  ///   The cache of known bots.
  /// </summary>
  public static ImmutableHashSet<string>? KnownBotListUserId { get; set; }

  /// <summary>
  ///   Determine if the rule is enabled in the configuration.
  /// </summary>
  /// <param name="user">The user.</param>
  /// <param name="config">The user's configuration.</param>
  /// <returns>True if it should run, false otherwise.</returns>
  public override bool ShouldRun(User user, TwitchUserConfig config) {
    return config is { Enabled: true, BanKnownBots: true };
  }

  /// <summary>
  ///   Searches for and bans known bots when they enter the chat.
  /// </summary>
  /// <param name="user">The user.</param>
  /// <param name="config">The user's configuration.</param>
  /// <param name="userProxy">The twitch api authenticated as the user we're scanning.</param>
  /// <param name="botProxy">The twitch api authenticated as the bot user.</param>
  /// <param name="db">The database.</param>
  /// <param name="stoppingToken">The cancellation token.</param>
  public override async Task Handle(User user, TwitchUserConfig config, TwitchApiProxy userProxy,
    TwitchApiProxy botProxy, NullinsideContext db, CancellationToken stoppingToken = new()) {
    if (null == user.TwitchId) {
      return;
    }

    // TODO: SKIP MODS AND VIPS

    // Get the list of people in the chat.
    List<Chatter>? chatters =
      (await botProxy.GetChattersInChannel(user.TwitchId, Constants.BotId, stoppingToken))?.ToList();
    if (null == chatters || chatters.Count == 0) {
      return;
    }

    // Perform the comparison in the lock to prevent multithreading issues.
    // The collection is extremely large so we do not want to make a copy of it.
    var botsInChatInsights = new List<Chatter>();
    var botsInChatCommanderRoot = new List<Chatter>();
    ImmutableHashSet<string>? knownBotUsernames = KnownBotListUsername;
    ImmutableHashSet<string>? knownBotUserIds = KnownBotListUserId;

    if (null != knownBotUsernames) {
      botsInChatInsights.AddRange(chatters.Where(k =>
        knownBotUsernames.Contains(k.UserLogin.ToLowerInvariant())));
    }

    if (null != knownBotUserIds) {
      botsInChatCommanderRoot.AddRange(chatters.Where(k =>
        knownBotUserIds.Contains(k.UserId.ToLowerInvariant())));
    }

    // Remove the whitelisted bots
    botsInChatInsights = botsInChatInsights
      .Where(b => !Constants.WhitelistedBots.Contains(b.UserLogin.ToLowerInvariant())).ToList();
    botsInChatCommanderRoot = botsInChatCommanderRoot
      .Where(b => !Constants.WhitelistedBots.Contains(b.UserLogin.ToLowerInvariant())).ToList();

    // Ban them.
    if (botsInChatInsights.Count != 0) {
      await BanOnce(botProxy, db, user.TwitchId, botsInChatInsights.Select(b => (Id: b.UserId, Username: b.UserLogin)),
        "[Bot] Username on Known Bot List", stoppingToken);
    }

    if (botsInChatCommanderRoot.Count != 0) {
      await BanOnce(botProxy, db, user.TwitchId,
        botsInChatCommanderRoot.Select(b => (Id: b.UserId, Username: b.UserLogin)),
        "[Bot] Username on Known Bot List", stoppingToken);
    }
  }

  /// <summary>
  ///   Gets the list of all known bots.
  /// </summary>
  /// <returns>The list of bots if successful, null otherwise.</returns>
  private static async Task<ImmutableHashSet<string>?> GetTwitchInsightsBots() {
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    // Reach out to the api and find out what bots are online.
    using var http = new HttpClient();
    HttpResponseMessage response = await http.GetAsync("https://api.twitchinsights.net/v1/bots/all");
    if (!response.IsSuccessStatusCode) {
      return null;
    }

    byte[]? content = await response.Content.ReadAsByteArrayAsync();
    string? jsonString = Encoding.UTF8.GetString(content);
    var liveBotsResponse = JsonConvert.DeserializeObject<TwitchInsightsLiveBotsResponse>(jsonString);
    if (null == liveBotsResponse) {
      return null;
    }

    ImmutableHashSet<string> allBots = liveBotsResponse.bots
      .Where(s => !string.IsNullOrWhiteSpace(s[0].ToString()))
#pragma warning disable 8602
      .Select(s => s[0].ToString().ToLowerInvariant())
#pragma warning restore 8602
      .ToImmutableHashSet();

    foreach (List<object>? list in liveBotsResponse.bots) {
      list.Clear();
    }

    liveBotsResponse.bots.Clear();
    liveBotsResponse.bots = null;
    liveBotsResponse = null;
    content = null;
    jsonString = null;
    return allBots;
  }

  /// <summary>
  ///   Gets the list of all known bots.
  /// </summary>
  /// <returns>The list of bots if successful, null otherwise.</returns>
  private static async Task<ImmutableHashSet<string>?> GetCommanderRootBots() {
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    // Reach out to the api and find out what bots are online.
    using var http = new HttpClient();
    using HttpResponseMessage response =
      await http.GetAsync("https://twitch-tools.rootonline.de/blocklist_manager.php?preset=known_bot_users");
    if (!response.IsSuccessStatusCode) {
      return null;
    }

    byte[]? content = await response.Content.ReadAsByteArrayAsync();
    string? jsonString = Encoding.UTF8.GetString(content);
    var liveBotsResponse = JsonConvert.DeserializeObject<List<string>>(jsonString);
    if (null == liveBotsResponse) {
      return null;
    }

    ImmutableHashSet<string> allBots = liveBotsResponse
#pragma warning disable 8602
      .Select(s => s.ToLowerInvariant())
#pragma warning restore 8602
      .ToImmutableHashSet();

    liveBotsResponse.Clear();
    liveBotsResponse = null;
    liveBotsResponse = null;
    content = null;
    jsonString = null;
    return allBots;
  }
}