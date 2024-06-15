using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Bots;

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
#if DEBUG
      return;
#endif

      while (!stoppingToken.IsCancellationRequested) {
        try {
          {
            string? token;
            {
              using IServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
              var db = scope.ServiceProvider.GetRequiredService<NullinsideContext>();
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

            TwitchClientProxy.Instance.TwitchUsername = Constants.BotUsername;
            TwitchClientProxy.Instance.TwitchOAuthToken = token;
          }

          botRules = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IBotRule).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(t => Activator.CreateInstance(t) as IBotRule)
            .Where(o => null != o)
            .ToArray()!;

          {
            using IServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
            foreach (IBotRule rule in botRules) {
              rule.Log = scope.ServiceProvider.GetRequiredService<ILogger<IBotRule>>();
            }
          }

          await Main(stoppingToken);
        }
        catch (Exception ex) {
          _log.LogError("Main Failed: {ex}", ex);
        }
      }
    }, stoppingToken);
  }

  /// <summary>
  ///   The starting point for running the bots functionality.
  /// </summary>
  /// <param name="stoppingToken">The stopping token.</param>
  private async Task Main(CancellationToken stoppingToken) {
#if DEBUG
    return;
#endif

    try {
      while (!stoppingToken.IsCancellationRequested) {
        bool didScan = false;
        using IServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NullinsideContext>();

        List<User>? users = await
          (from user in db.Users
            where user.TwitchId != Constants.BotId &&
                  !user.IsBanned
            select user)
          .Include(u => u.TwitchConfig)
          .Where(u => null != u.TwitchConfig && u.TwitchConfig.Enabled)
          .AsNoTracking()
          .ToListAsync(stoppingToken);

        User? botUser = await db.Users.AsNoTracking()
          .FirstOrDefaultAsync(u => u.TwitchId == Constants.BotId, stoppingToken);
        if (null == botUser) {
          throw new Exception("No bot user in database");
        }

        foreach (User user in users) {
          // Determine if it's too early for a scan.
          if (DateTime.UtcNow < user.TwitchLastScanned + Constants.MinimumTimeBetweenScansLive) {
            continue;
          }

          // Get the API
          TwitchApiProxy? botApi = await GetApiAndRefreshToken(botUser, db, stoppingToken);
          if (null == botRules || null == user.TwitchConfig || null == botApi) {
            continue;
          }

          // Run the rules that scan the chats and the accounts.
          foreach (IBotRule rule in botRules) {
            try {
              if (rule.ShouldRun(user, user.TwitchConfig)) {
                await rule.Handle(user, user.TwitchConfig, botApi, db, stoppingToken);
              }
            }
            catch (Exception e) {
              _log.LogError($"{user.TwitchUsername}: Failed to evaluate rule", e);
            }
          }

          // Log that we performed a scan to completion.
          User? dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id, stoppingToken);
          if (null == dbUser) {
            continue;
          }

          dbUser.TwitchLastScanned = DateTime.UtcNow;
          await db.SaveChangesAsync(stoppingToken);
          didScan = true;
        }

        if (!didScan) {
          await Task.Delay(Constants.MinimumTimeBetweenScansLive, stoppingToken);
        }
      }
    }
    catch (Exception ex) {
      _log.LogError("{message}", ex);
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
    if (DateTime.UtcNow < user.TwitchTokenExpiration + TimeSpan.FromHours(1)) {
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