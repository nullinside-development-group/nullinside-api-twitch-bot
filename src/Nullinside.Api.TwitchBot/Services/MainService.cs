﻿using System.Collections.Concurrent;

using log4net;

using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Common.Twitch.Json;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Bots;
using Nullinside.Api.TwitchBot.ChatRules;
using Nullinside.Api.TwitchBot.Model;

using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.Services;

/// <summary>
///   The starting point for running the bot's functionality.
/// </summary>
public class MainService : BackgroundService {
  /// <summary>
  ///   The amount of time to wait between each scan of the live users, in milliseconds.
  /// </summary>
  private const int SCAN_LOOP_DELAY_MILLISECONDS = 10000;

  /// <summary>
  ///   The bot rules to scan with.
  /// </summary>
  private static IBotRule[]? s_botRules;

  /// <summary>
  ///   The twitch api.
  /// </summary>
  private readonly ITwitchApiProxy _api;

  /// <summary>
  ///   Handles the enforcing rules on chat messages.
  /// </summary>
  private readonly TwitchChatMessageMonitorConsumer _chatMessageConsumer;

  /// <summary>
  ///   The twitch client for sending/receiving chat messages.
  /// </summary>
  private readonly ITwitchClientProxy _client;

  /// <summary>
  ///   The database.
  /// </summary>
  private readonly INullinsideContext _db;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILog _log = LogManager.GetLogger(typeof(MainService));

  /// <summary>
  ///   A collection of all bans received.
  /// </summary>
  /// <remarks>
  ///   We keep these to cross-reference with the <see cref="_receivedMessages" /> so we can detect possible
  ///   bot messages. This helps identify bots like the ones that post "Click here for viewers: scam.com".
  /// </remarks>
  private readonly List<OnUserBannedArgs> _receivedBans = new();

  /// <summary>
  ///   The queue of twitch chat messages consumed by <see cref="_chatMessageConsumer" /> for the enforcement of rules.
  /// </summary>
  private readonly BlockingCollection<ChatMessage> _receivedMessageProcessingQueue = new();

  /// <summary>
  ///   A collection of all chat messages.
  /// </summary>
  /// <remarks>
  ///   We keep these to cross-reference with the <see cref="_receivedBans" /> so we can detect possible
  ///   bot messages. This helps identify bots like the ones that post "Click here for viewers: scam.com".
  /// </remarks>
  private readonly List<OnMessageReceivedArgs> _receivedMessages = new();

  /// <summary>
  ///   The service scope.
  /// </summary>
  private readonly IServiceScope _scope;

  /// <summary>
  ///   The service scope factory.
  /// </summary>
  private readonly IServiceScopeFactory _serviceScopeFactory;

  /// <summary>
  ///   Initializes a new instance of the <see cref="MainService" /> class.
  /// </summary>
  /// <param name="serviceScopeFactory">The service scope factory.</param>
  public MainService(IServiceScopeFactory serviceScopeFactory) {
    _serviceScopeFactory = serviceScopeFactory;
    _scope = _serviceScopeFactory.CreateScope();
    _db = _scope.ServiceProvider.GetRequiredService<INullinsideContext>();
    _api = _scope.ServiceProvider.GetRequiredService<ITwitchApiProxy>();
    _client = _scope.ServiceProvider.GetRequiredService<ITwitchClientProxy>();
    _client.AddDisconnectedCallback(OnTwitchClientDisconected);
    _chatMessageConsumer = new TwitchChatMessageMonitorConsumer(_db, _api, _receivedMessageProcessingQueue);
  }

  /// <summary>
  ///   Called when the twitch client is disconnected.
  /// </summary>
  private void OnTwitchClientDisconected() {
    _log.Info("Twitch Client Disconnected, exiting app");
    Environment.Exit(0);
  }

  /// <summary>
  ///   The execute called by the .NET runtime.
  /// </summary>
  /// <param name="stoppingToken">The stopping token.</param>
  protected override Task ExecuteAsync(CancellationToken stoppingToken) {
    return Task.Run(async () => {
      while (!stoppingToken.IsCancellationRequested) {
        try {
          ITwitchApiProxy? botApi = await _db.ConfigureBotApiAndRefreshToken(_api, stoppingToken).ConfigureAwait(false);
          if (null == botApi || !await botApi.GetAccessTokenIsValid(stoppingToken).ConfigureAwait(false)) {
            throw new Exception("Unable to log in as bot user");
          }

          _client.TwitchUsername = Constants.BOT_USERNAME;
          _client.TwitchOAuthToken = botApi.OAuth?.AccessToken;

          s_botRules = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IBotRule).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .Select(t => Activator.CreateInstance(t) as IBotRule)
            .Where(o => null != o)
            .ToArray()!;

          await Main(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex) {
          _log.Error("Main Failed", ex);
          await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
        }
      }
    }, stoppingToken);
  }

  /// <summary>
  ///   The starting point for running the bots' functionality.
  /// </summary>
  /// <param name="stoppingToken">The stopping token.</param>
  private async Task Main(CancellationToken stoppingToken = new()) {
    try {
      while (!stoppingToken.IsCancellationRequested) {
        using (IServiceScope scope = _serviceScopeFactory.CreateAsyncScope()) {
          var db = scope.ServiceProvider.GetRequiredService<INullinsideContext>();
          await using (db.ConfigureAwait(false)) {
            // Send logs to database
            DumpLogsToDatabase(db);

            // Get the list of users with the bot enabled.
            List<User>? usersWithBotEnabled = await GetUsersWithBotEnabled(db, stoppingToken).ConfigureAwait(false);
            if (null == usersWithBotEnabled) {
              continue;
            }

            // Get the bot user's information.
            User? botUser = await db.Users.AsNoTracking()
              .FirstOrDefaultAsync(u => u.TwitchId == Constants.BOT_ID, stoppingToken).ConfigureAwait(false);
            if (null == botUser) {
              throw new Exception("No bot user in database");
            }

            ITwitchApiProxy? botApi = await db.ConfigureApiAndRefreshToken(botUser, _api, stoppingToken).ConfigureAwait(false);
            if (null != botApi) {
              // Ensure the twitch client has the most up-to-date password
              _client.TwitchOAuthToken = botApi.OAuth?.AccessToken;

              // Trim channels that aren't live
              IEnumerable<string> liveUsers = await botApi.GetChannelsLive(usersWithBotEnabled
                .Where(u => null != u.TwitchId)
                .Select(u => u.TwitchId)!).ConfigureAwait(false);
              usersWithBotEnabled = usersWithBotEnabled.Where(u => liveUsers.Contains(u.TwitchId)).ToList();

              // Trim channels we aren't a mod in
              IEnumerable<TwitchModeratedChannel> moddedChannels = await botApi.GetUserModChannels(Constants.BOT_ID).ConfigureAwait(false);
              usersWithBotEnabled = usersWithBotEnabled
                .Where(u => moddedChannels.Select(m => m.broadcaster_id).Contains(u.TwitchId))
                .ToList();

              // Join all the channels we're a mod in. Why do we limit it to channels we are a mod in? Twitch changed
              // its chat limits so that "verified bots" like us don't get special treatment anymore. The only thing
              // that skips the chat limits is if it's a channel you're a mod in.
              foreach (User channel in usersWithBotEnabled) {
                if (string.IsNullOrWhiteSpace(channel.TwitchUsername)) {
                  continue;
                }

                await _client.AddMessageCallback(channel.TwitchUsername, OnTwitchMessageReceived).ConfigureAwait(false);
                await _client.AddBannedCallback(channel.TwitchUsername, OnTwitchBanReceived).ConfigureAwait(false);
              }
            }

            // Spawn 5 workers to process all the live user's channels.
            Parallel.ForEach(usersWithBotEnabled, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async user => {
              try {
                await DoScan(user, botUser, stoppingToken).ConfigureAwait(false);
              }
              catch (Exception ex) {
                _log.Error($"Scan failed for {user.TwitchUsername}", ex);
              }
            });
          }
        }

        // Wait between scans. We don't want to spam the API too much.
        await Task.Delay(SCAN_LOOP_DELAY_MILLISECONDS, stoppingToken).ConfigureAwait(false);
      }
    }
    catch (Exception ex) {
      _log.Error("Main Inner failed", ex);
    }
  }

  /// <summary>
  ///   Dumps a record of the current batch of twitch bans and twitch messages into the database.
  /// </summary>
  /// <param name="db">The database.</param>
  private void DumpLogsToDatabase(INullinsideContext db) {
    lock (_receivedBans) {
      db.TwitchUserBannedOutsideOfBotLogs.AddRange(_receivedBans.Select(b => new TwitchUserBannedOutsideOfBotLogs {
        Channel = b.UserBan.Channel,
        Reason = b.UserBan.BanReason,
        Timestamp = DateTime.UtcNow,
        TwitchId = b.UserBan.TargetUserId,
        TwitchUsername = b.UserBan.Username
      }));

      _receivedBans.Clear();
    }

    lock (_receivedMessages) {
      db.TwitchUserChatLogs.AddRange(_receivedMessages.Select(m => new TwitchUserChatLogs {
        Channel = m.ChatMessage.Channel,
        TwitchId = m.ChatMessage.UserId,
        TwitchUsername = m.ChatMessage.Username,
        Message = m.ChatMessage.Message,
        Timestamp = DateTime.UtcNow // TODO: Convert the tmi-ts to a datetime.
      }));

      _receivedMessages.Clear();
    }

    db.SaveChanges();
  }

  /// <summary>
  ///   Records bans for all of our user's chats.
  /// </summary>
  /// <param name="e">The ban.</param>
  private void OnTwitchBanReceived(OnUserBannedArgs e) {
    lock (_receivedBans) {
      _receivedBans.Add(e);
    }
  }

  /// <summary>
  ///   Records chat messages for all our user's chats.
  /// </summary>
  /// <param name="e">The chat message.</param>
  private void OnTwitchMessageReceived(OnMessageReceivedArgs e) {
    _receivedMessageProcessingQueue.Add(e.ChatMessage);

    lock (_receivedMessages) {
      _receivedMessages.Add(e);
    }
  }

  /// <summary>
  ///   Retrieve all users that have the bot enabled.
  /// </summary>
  /// <param name="db">The database.</param>
  /// <param name="stoppingToken">The stopping token.</param>
  /// <returns>The list of users with the bot enabled.</returns>
  private async Task<List<User>?> GetUsersWithBotEnabled(INullinsideContext db, CancellationToken stoppingToken) {
    return await
      (from user in db.Users
        orderby user.TwitchLastScanned
        where user.TwitchId != Constants.BOT_ID &&
              !user.IsBanned
        select user)
      .Include(u => u.TwitchConfig)
      .Where(u => null != u.TwitchConfig && u.TwitchConfig.Enabled)
      .AsNoTracking()
      .ToListAsync(stoppingToken).ConfigureAwait(false);
  }

  /// <summary>
  ///   Retrieve all users that are banned from using the bot.
  /// </summary>
  /// <param name="db">The database.</param>
  /// <param name="stoppingToken">The stopping token.</param>
  /// <returns>The list of users with the bot enabled.</returns>
  private async Task<List<User>?> GetBannedUsers(INullinsideContext db, CancellationToken stoppingToken) {
    return await
      (from user in db.Users
        orderby user.TwitchLastScanned
        where user.TwitchId != Constants.BOT_ID &&
              user.IsBanned
        select user)
      .AsNoTracking()
      .ToListAsync(stoppingToken).ConfigureAwait(false);
  }

  /// <summary>
  ///   Performs the scan on a user.
  /// </summary>
  /// <param name="user">The user to scan.</param>
  /// <param name="botUser">The bot user.</param>
  /// <param name="stoppingToken">The stopping token.</param>
  private async Task DoScan(User user, User botUser, CancellationToken stoppingToken) {
    // Determine if it's too early for a scan.
    if (DateTime.UtcNow < user.TwitchLastScanned + Constants.MINIMUM_TIME_BETWEEN_SCANS_LIVE) {
      return;
    }

    // Since each scan will happen on a separate thread, we need an individual scope and database reference
    // per invocation, allowing them to release each loop.
    using (IServiceScope scope = _serviceScopeFactory.CreateAsyncScope()) {
      var db = scope.ServiceProvider.GetRequiredService<INullinsideContext>();
      await using (db.ConfigureAwait(false)) {
        // Get the API
        _api.Configure(botUser);
        if (null == s_botRules || null == user.TwitchConfig) {
          return;
        }

        // Run the rules that scan the chats and the accounts.
        foreach (IBotRule rule in s_botRules) {
          try {
            if (rule.ShouldRun(user.TwitchConfig)) {
              await rule.Handle(user, user.TwitchConfig, _api, db, stoppingToken).ConfigureAwait(false);
            }
          }
          catch (Exception e) {
            _log.Error($"{user.TwitchUsername}: Failed to evaluate rule", e);
          }
        }

        // Log that we performed a scan to completion.
        User? dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id, stoppingToken).ConfigureAwait(false);
        if (null == dbUser) {
          return;
        }

        dbUser.TwitchLastScanned = DateTime.UtcNow;
        await db.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
      }
    }
  }
}