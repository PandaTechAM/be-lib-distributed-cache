namespace DistributedCache.Services.Interfaces;

public interface IDistributedLockService
{
   Task<bool> AcquireLockAsync(string resourceKey, string lockToken);
   Task<bool> HasLockAsync(string resourceKey);
   Task WaitUntilLockIsReleasedAsync(string resourceKey, CancellationToken cancellationToken);
   Task ReleaseLockAsync(string resourceKey, string lockToken);
}