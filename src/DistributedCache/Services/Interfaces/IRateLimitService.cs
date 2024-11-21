using DistributedCache.Dtos;

namespace DistributedCache.Services.Interfaces;

/// <summary>
///    Defines a service for applying rate limiting to specific operations.
/// </summary>
/// <typeparam name="T">
///    The type of the class that is used to infer the assembly name for key isolation.
///    Even if the assembly name is not used in key prefixing, this type must still be provided
///    to comply with the service's requirements.
/// </typeparam>
public interface IRateLimitService<T>
   where T : class
{
   /// <summary>
   ///    Applies rate limiting based on the provided configuration.
   /// </summary>
   /// <param name="rateLimitConfiguration">
   ///    The configuration defining rate limit rules, such as maximum attempts and
   ///    expiration.
   /// </param>
   /// <param name="cancellationToken">
   ///    A token that can be used to propagate notification that the operation should be
   ///    canceled.
   /// </param>
   /// <returns>
   ///    A task representing the asynchronous operation, containing the result of the rate limiting operation,
   ///    which indicates whether the rate limit has been exceeded or not.
   /// </returns>
   ValueTask<RateLimitState> RateLimitAsync(RateLimitConfiguration rateLimitConfiguration,
      CancellationToken cancellationToken = default);
}