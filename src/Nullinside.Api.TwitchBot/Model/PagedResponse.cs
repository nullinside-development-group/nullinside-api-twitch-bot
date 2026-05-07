namespace Nullinside.Api.TwitchBot.Model;

/// <summary>
///   A paged response.
/// </summary>
/// <typeparam name="T">The type of the data.</typeparam>
public class PagedResponse<T> {
  /// <summary>
  ///   The data.
  /// </summary>
  public IEnumerable<T> Data { get; set; } = [];

  /// <summary>
  ///   The total number of items.
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  ///   The current page.
  /// </summary>
  public int Page { get; set; }

  /// <summary>
  ///   The page size.
  /// </summary>
  public int PageSize { get; set; }
}
