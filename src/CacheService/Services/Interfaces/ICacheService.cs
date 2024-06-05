namespace CacheService.Services.Interfaces;

/// <summary>
/// Interface for cache service operations.
/// </summary>
/// <typeparam name="T">The type of the cache entity.</typeparam>
public interface ICacheService<T> where T : class
{
   /// <summary>
   /// Gets or creates a cache entry asynchronously.
   /// </summary>
   /// <param name="key">The key of the cache entry.</param>
   /// <param name="factory">A factory function to create the cache entry if it does not exist.</param>
   /// <param name="expiration">Optional expiration time for the cache entry.</param>
   /// <param name="tags">Optional tags associated with the cache entry.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation, with the cache entry as the result.</returns>
   ValueTask<T> GetOrCreateAsync(string key, Func<CancellationToken, ValueTask<T>> factory,
      TimeSpan? expiration = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default);

   /// <summary>
   /// Gets a cache entry asynchronously.
   /// </summary>
   /// <param name="key">The key of the cache entry.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation, with the cache entry as the result if found; otherwise, null.</returns>
   ValueTask<T?> GetAsync(string key, CancellationToken token = default);

   /// <summary>
   /// Sets a cache entry asynchronously.
   /// </summary>
   /// <param name="key">The key of the cache entry.</param>
   /// <param name="value">The value of the cache entry.</param>
   /// <param name="expiration">Optional expiration time for the cache entry.</param>
   /// <param name="tags">Optional tags associated with the cache entry.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation.</returns>
   ValueTask SetAsync(string key, T value, TimeSpan? expiration = null, IReadOnlyCollection<string>? tags = null,
      CancellationToken token = default);

   /// <summary>
   /// Removes a cache entry by key asynchronously.
   /// </summary>
   /// <param name="key">The key of the cache entry to remove.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation.</returns>
   ValueTask RemoveByKeyAsync(string key, CancellationToken token = default);

   /// <summary>
   /// Removes multiple cache entries by their keys asynchronously.
   /// </summary>
   /// <param name="keys">The keys of the cache entries to remove.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation.</returns>
   ValueTask RemoveByKeysAsync(IEnumerable<string> keys, CancellationToken token = default);

   /// <summary>
   /// Removes cache entries associated with a tag asynchronously.
   /// </summary>
   /// <param name="tag">The tag associated with the cache entries to remove.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation.</returns>
   /// <remarks>
   /// If multiple tags are specified, any entry matching any one of the tags will be removed. This means tags are treated as an "OR" condition.
   /// </remarks>
   ValueTask RemoveByTagAsync(string tag, CancellationToken token = default);

   /// <summary>
   /// Removes cache entries associated with multiple tags asynchronously.
   /// </summary>
   /// <param name="tags">The tags associated with the cache entries to remove.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation.</returns>
   /// <remarks>
   /// If multiple tags are specified, any entry matching any one of the tags will be removed. This means tags are treated as an "OR" condition.
   /// </remarks>
   ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken token = default);
}
