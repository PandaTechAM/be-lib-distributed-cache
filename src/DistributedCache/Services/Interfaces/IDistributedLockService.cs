namespace DistributedCache.Services.Interfaces;

/// <summary>Provides distributed locking primitives backed by Redis.</summary>
public interface IDistributedLockService
{
    /// <summary>Attempt to acquire the lock for a resource. Returns false if it is already held.</summary>
    Task<bool> AcquireLockAsync(string resourceKey, string lockToken);

    /// <summary>Return whether the resource is currently locked.</summary>
    Task<bool> HasLockAsync(string resourceKey);

    /// <summary>Wait until the lock on a resource is released or the operation is cancelled.</summary>
    Task WaitUntilLockIsReleasedAsync(string resourceKey, CancellationToken cancellationToken);

    /// <summary>Release the lock only if the supplied token matches the current holder.</summary>
    Task ReleaseLockAsync(string resourceKey, string lockToken);
}
