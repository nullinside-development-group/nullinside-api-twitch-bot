using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.TwitchBot.Controllers;

/// <summary>
///   Extends the <see cref="ControllerBase" /> class from .NET.
/// </summary>
public static class ControllerBaseExtensions {
  /// <summary>
  ///   Gets the user ID from the authenticated user.
  /// </summary>
  /// <param name="controller">The controller.</param>
  /// <returns>The user id if successful, null otherwise.</returns>
  public static int? GetUserId(this ControllerBase controller) {
    Claim? userIdClaim = controller.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);
    if (null == userIdClaim) {
      return null;
    }

    return int.Parse(userIdClaim.Value);
  }

  /// <summary>
  ///   Gets the user email from the authenticated user.
  /// </summary>
  /// <param name="controller">The controller.</param>
  /// <returns>The user's email if successful, null otherwise.</returns>
  public static string? GetUserEmail(this ControllerBase controller) {
    Claim? emailClaim = controller.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
    if (null == emailClaim) {
      return null;
    }

    return emailClaim.Value;
  }

  /// <summary>
  ///   Gets the user role from the authenticated user.
  /// </summary>
  /// <param name="controller">The controller.</param>
  /// <returns>The user's role if successful, null otherwise.</returns>
  public static string? GetUserRole(this ControllerBase controller) {
    Claim? roleClaim = controller.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
    if (null == roleClaim) {
      return null;
    }

    return roleClaim.Value;
  }

  /// <summary>
  ///   Gets the user database object from the authenticated user.
  /// </summary>
  /// <param name="controller">The controller.</param>
  /// <param name="dbContext">The database context.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The user database entity if successful, null otherwise.</returns>
  public static async Task<User?> GetUserEntity(this ControllerBase controller, INullinsideContext dbContext, CancellationToken token = new()) {
    int? userId = controller.GetUserId();
    if (null == userId) {
      return null;
    }

    return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsBanned, token).ConfigureAwait(false);
  }
}