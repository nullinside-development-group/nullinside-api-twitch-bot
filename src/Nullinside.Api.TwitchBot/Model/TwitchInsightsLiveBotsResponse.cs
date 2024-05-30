﻿// <auto-generated/>
namespace Nullinside.Api.TwitchBot.Model;

using System.Collections.Generic;

/// <summary>
///     Response from the twitch insights API about what bots are active.
/// </summary>
internal class TwitchInsightsLiveBotsResponse {
  /// <summary>
  ///     The list of bots.
  /// </summary>
  /// <remarks>
  ///     The format of this is super weird. The list is actually tuple-like construct that has the following positions:
  ///     0: The username
  ///     1: A number
  ///     2: A number
  /// </remarks>
  public List<List<object>> bots { get; set; }

  /// <summary>
  ///     The total number of live bots.
  /// </summary>
  public int _total { get; set; }
}