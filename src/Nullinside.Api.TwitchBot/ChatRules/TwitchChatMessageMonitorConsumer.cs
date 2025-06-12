using System.Collections.Concurrent;

using log4net;

using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Model;

using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Client.Models;

namespace Nullinside.Api.TwitchBot.ChatRules;

/// <summary>
///   Scans twitch messages for users to ban.
/// </summary>
public class TwitchChatMessageMonitorConsumer : IDisposable {
  /// <summary>
  ///   The time between loops when there is nothing to do.
  /// </summary>
  private const int LOOP_TIMEOUT = 500;

  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog LOG = LogManager.GetLogger(typeof(TwitchChatMessageMonitorConsumer));

  /// <summary>
  ///   The rules to scan messages with.
  /// </summary>
  private static IChatRule[]? s_chatRules;

  /// <summary>
  ///   The twitch api.
  /// </summary>
  private readonly ITwitchApiProxy _api;

  /// <summary>
  ///   The nullinside database.
  /// </summary>
  private readonly INullinsideContext _db;

  /// <summary>
  ///   The non-priority queue to scan messages from.
  /// </summary>
  private readonly BlockingCollection<ChatMessage> _queue;

  /// <summary>
  ///   The thread responsible for scanning channels for bots.
  /// </summary>
  private readonly Thread _thread;

  /// <summary>
  ///   The poison pill to kill the <see cref="_thread" />.
  /// </summary>
  private bool _poisonPill;

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatMessageMonitorConsumer" /> class.
  /// </summary>
  /// <param name="db">The database.</param>
  /// <param name="api">The twitch api.</param>
  /// <param name="queue">The non-priority queue to scan messages from.</param>
  public TwitchChatMessageMonitorConsumer(INullinsideContext db, ITwitchApiProxy api, BlockingCollection<ChatMessage> queue) {
    _db = db;
    _queue = queue;
    _api = api;

    _thread = new Thread(MainLoop) {
      IsBackground = true,
      Name = "TwitchChatMessageMonitorConsumer"
    };
    _thread.Start();
  }

  /// <summary>
  ///   Releases unmanaged resources.
  /// </summary>
  public void Dispose() {
    _poisonPill = true;
    if (!_thread.Join(30000)) {
      _thread.Interrupt();
    }

    _queue.Dispose();
  }

  /// <summary>
  ///   Handles scanning channels for bots.
  /// </summary>
  private async void MainLoop() {
    if (null == s_chatRules) {
      // Dynamically initializes all of the rules.
#pragma warning disable 8619
      s_chatRules = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t => typeof(IChatRule).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
        .Select(t => Activator.CreateInstance(t) as IChatRule)
        .Where(o => null != o)
        .ToArray();
#pragma warning restore 8619
    }

    while (!_poisonPill) {
      try {
        // Try to get a message from one of the two queues.
        ChatMessage? message;
        _queue.TryTake(out message);

        // If we didn't get a message, loop.
        if (null == message) {
          Thread.Sleep(LOOP_TIMEOUT);
          continue;
        }

        string? channel = message.Channel;
        try {
          // Sanity check.
          if (string.IsNullOrWhiteSpace(message.Channel) ||
              string.IsNullOrWhiteSpace(message.Message)) {
            continue;
          }

          // We need the user's configuration to check which rules to run.
          User? user = _db.Users
            .Include(u => u.TwitchConfig)
            .FirstOrDefault(u =>
              !u.IsBanned &&
              null != u.TwitchConfig &&
              u.TwitchConfig.Enabled &&
              u.TwitchUsername == message.Channel
            );

          if (null == user?.TwitchConfig || null == user.TwitchId) {
            continue;
          }

          // Get all the rules onto the stack.
          IChatRule[]? rules = s_chatRules?.ToArray();
          if (null == rules) {
            continue;
          }

          // Get the bot proxy
          ITwitchApiProxy? botProxy = await _db.ConfigureBotApiAndRefreshToken(_api);
          if (null == botProxy) {
            continue;
          }

          // Process each rule.
          foreach (IChatRule rule in rules) {
            try {
              if (rule.ShouldRun(user.TwitchConfig)) {
                if (!await rule.Handle(user.TwitchId, botProxy, new TwitchChatMessage(message), _db)) {
                  break;
                }
              }
            }
            catch (Exception e) {
              LOG.Error($"{channel}: Failed to evaluate rule on {message.Username}({message.UserId}): {message.Message}", e);
            }
          }
        }
        catch (BadScopeException) {
          // This exception means almost nothing...maybe the OAuth token really is invalid....
          // maybe it isn't. The library sends this as a generic reason for a failure. We will
          // put our best foot forward and try to refresh the token and then give up.
          LOG.Error($"Bad Credentials: {channel}");
        }
        catch (Exception e) {
          LOG.Error($"{channel}: Unhandled exception outside of rule", e);
        }
      }
      catch (Exception ex) {
        LOG.Error("Unhandled exception in consumer", ex);
      }
    }
  }
}