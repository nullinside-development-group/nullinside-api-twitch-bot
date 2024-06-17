using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Common.Twitch.Json;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Bots;

using TwitchLib.Client.Events;

namespace Nullinside.Api.TwitchBot.Services;

/// <summary>
///   The starting point for running the bot's functionality.
/// </summary>
public class MainService : BackgroundService {
  /// <summary>
  ///   The bot rules to scan with.
  /// </summary>
  private static IBotRule[]? botRules;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILogger<MainService> _log;

  /// <summary>
  ///   A collection of all bans received.
  /// </summary>
  /// <remarks>
  ///   We keep these to cross-reference with the <see cref="_receivedMessages" /> so we can detect possible
  ///   bot messages. This helps identify bots like the ones that post "Click here for viewers: scam.com".
  /// </remarks>
  private readonly List<OnUserBannedArgs> _receivedBans = new();

  /// <summary>
  ///   A collection of all chat messages.
  /// </summary>
  /// <remarks>
  ///   We keep these to cross-reference with the <see cref="_receivedBans" /> so we can detect possible
  ///   bot messages. This helps identify bots like the ones that post "Click here for viewers: scam.com".
  /// </remarks>
  private readonly List<OnMessageReceivedArgs> _receivedMessages = new();

  /// <summary>
  ///   The service scope factory.
  /// </summary>
  private readonly IServiceScopeFactory _serviceScopeFactory;

  /// <summary>
  ///   Initializes a new instance of the <see cref="MainService" /> class.
  /// </summary>
  /// <param name="logger">The logger.</param>
  /// <param name="serviceScopeFactory">The service scope factory.</param>
  public MainService(ILogger<MainService> logger, IServiceScopeFactory serviceScopeFactory) {
    _log = logger;
    _serviceScopeFactory = serviceScopeFactory;
  }

  /// <summary>
  ///   The execute called by the .NET runtime.
  /// </summary>
  /// <param name="stoppingToken">The stopping token.</param>
  protected override Task ExecuteAsync(CancellationToken stoppingToken) {
    return Task.Run(async () => {
      while (!stoppingToken.IsCancellationRequested) {
        try {
          {
            string? token;
            {
              using (IServiceScope scope = _serviceScopeFactory.CreateAsyncScope()) {
                await using (var db = scope.ServiceProvider.GetRequiredService<NullinsideContext>()) {
                  User? botUser =
                    await db.Users.FirstOrDefaultAsync(u => Constants.BotEmail.Equals(u.Email), stoppingToken);
                  if (null == botUser) {
                    throw new Exception("Bot User not found in Database");
                  }

                  TwitchApiProxy botApi = GetApi(botUser);
                  if (!await botApi.IsValid(stoppingToken)) {
                    throw new Exception("Unable to log in as bot user");
                  }

                  token = botUser.TwitchToken;
                }
              }
            }

            TwitchClientProxy.Instance.TwitchUsername = Constants.BotUsername;
            TwitchClientProxy.Instance.TwitchOAuthToken = token;
          }

          botRules = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IBotRule).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(t => Activator.CreateInstance(t) as IBotRule)
            .Where(o => null != o)
            .ToArray()!;

          await Main(stoppingToken);
        }
        catch (Exception ex) {
          _log.LogError(ex, "Main Failed");
        }
      }
    }, stoppingToken);
  }

  /// <summary>
  ///   The starting point for running the bots functionality.
  /// </summary>
  /// <param name="stoppingToken">The stopping token.</param>
  private async Task Main(CancellationToken stoppingToken) {
    try {
      while (!stoppingToken.IsCancellationRequested) {
        using (IServiceScope scope = _serviceScopeFactory.CreateAsyncScope()) {
          await using (var db = scope.ServiceProvider.GetRequiredService<NullinsideContext>()) {
            // Send logs to database
            DumpLogsToDatabase(db);

            // Get the list of users with the bot enabled.
            List<User>? usersWithBotEnabled = await GetUsersWithBotEnabled(db, stoppingToken);
            if (null == usersWithBotEnabled) {
              continue;
            }

            // Get the bot user's information.
            User? botUser = await db.Users.AsNoTracking()
              .FirstOrDefaultAsync(u => u.TwitchId == Constants.BotId, stoppingToken);
            if (null == botUser) {
              throw new Exception("No bot user in database");
            }

            TwitchApiProxy? botApi = await GetApiAndRefreshToken(botUser, db, stoppingToken);
            if (null != botApi) {
              // Trim channels that aren't live
              IEnumerable<string> liveUsers = await botApi.GetLiveChannels(usersWithBotEnabled
                .Where(u => null != u.TwitchId)
                .Select(u => u.TwitchId)!);
              usersWithBotEnabled = usersWithBotEnabled.Where(u => liveUsers.Contains(u.TwitchId)).ToList();

              // Trim channels we aren't a mod in
              IEnumerable<TwitchModeratedChannel> moddedChannels = await botApi.GetChannelsWeMod(Constants.BotId);
              usersWithBotEnabled = usersWithBotEnabled
                .Where(u => moddedChannels
                  .Select(m => m.broadcaster_id)
                  .Contains(u.TwitchId))
                .ToList();

              // Join all the channels we're a mod in
              TwitchClientProxy.Instance.TwitchUsername = Constants.BotUsername;
              TwitchClientProxy.Instance.TwitchOAuthToken = botApi.AccessToken;

              foreach (TwitchModeratedChannel channel in moddedChannels) {
                await TwitchClientProxy.Instance.AddMessageCallback(channel.broadcaster_login, Callback);
                await TwitchClientProxy.Instance.AddBannedCallback(channel.broadcaster_login, Callback);
              }
            }

            Parallel.ForEach(usersWithBotEnabled, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async user => {
              try {
                await DoScan(user, botUser, stoppingToken);
              }
              catch (Exception ex) {
                _log.LogError(ex, $"Scan failed for {user.TwitchUsername}");
              }
            });

            await Task.Delay(10000, stoppingToken);
          }
        }
      }
    }
    catch (Exception ex) {
      _log.LogError(ex, "Main Inner failed");
    }
  }

  private void DumpLogsToDatabase(NullinsideContext db) {
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

  private void Callback(OnUserBannedArgs obj) {
    lock (_receivedBans) {
      _receivedBans.Add(obj);
    }
  }

  private void Callback(OnMessageReceivedArgs obj) {
    lock (_receivedMessages) {
      _receivedMessages.Add(obj);
    }
  }

  private async Task<List<User>?> GetUsersWithBotEnabled(NullinsideContext db, CancellationToken stoppingToken) {
    return await
      (from user in db.Users
        orderby user.TwitchLastScanned
        where user.TwitchId != Constants.BotId &&
              !user.IsBanned
        select user)
      .Include(u => u.TwitchConfig)
      .Where(u => null != u.TwitchConfig && u.TwitchConfig.Enabled)
      .AsNoTracking()
      .ToListAsync(stoppingToken);
  }

  private async Task DoScan(User user, User botUser, CancellationToken stoppingToken) {
    // Determine if it's too early for a scan.
    if (DateTime.UtcNow < user.TwitchLastScanned + Constants.MinimumTimeBetweenScansLive) {
      return;
    }

    using (IServiceScope scope = _serviceScopeFactory.CreateAsyncScope()) {
      await using (var db = scope.ServiceProvider.GetRequiredService<NullinsideContext>()) {
        // Get the API
        TwitchApiProxy? botApi = await GetApiAndRefreshToken(botUser, db, stoppingToken);
        if (null == botRules || null == user.TwitchConfig || null == botApi) {
          return;
        }

        // Run the rules that scan the chats and the accounts.
        foreach (IBotRule rule in botRules) {
          try {
            if (rule.ShouldRun(user, user.TwitchConfig)) {
              await rule.Handle(user, user.TwitchConfig, botApi, db, stoppingToken);
            }
          }
          catch (Exception e) {
            _log.LogError(e, $"{user.TwitchUsername}: Failed to evaluate rule");
          }
        }

        // Log that we performed a scan to completion.
        User? dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id, stoppingToken);
        if (null == dbUser) {
          return;
        }

        dbUser.TwitchLastScanned = DateTime.UtcNow;
        await db.SaveChangesAsync(stoppingToken);
      }
    }
  }

  private static TwitchApiProxy GetApi(User user) {
    return new TwitchApiProxy {
      AccessToken = user.TwitchToken,
      RefreshToken = user.TwitchRefreshToken,
      ExpiresUtc = user.TwitchTokenExpiration
    };
  }

  private static async Task<TwitchApiProxy?> GetApiAndRefreshToken(User user, NullinsideContext db,
    CancellationToken stoppingToken = new()) {
    // Get the API
    TwitchApiProxy api = GetApi(user);

    // Refresh its token if necessary.
    if (DateTime.UtcNow + TimeSpan.FromHours(1) > user.TwitchTokenExpiration) {
      if (await api.RefreshTokenAsync(stoppingToken)) {
        User? row = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id, stoppingToken);
        if (null == row) {
          return null;
        }

        row.TwitchToken = api.AccessToken;
        row.TwitchRefreshToken = api.RefreshToken;
        row.TwitchTokenExpiration = api.ExpiresUtc;
        await db.SaveChangesAsync(stoppingToken);
      }
    }

    return api;
  }
}