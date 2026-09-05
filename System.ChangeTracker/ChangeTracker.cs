using System.ChangeTracker.Snapshots;
using System.Runtime.CompilerServices;

namespace System.ChangeTracker;

/// <summary>
/// Remembers what an object looked like, then tells you what changed. Objects are held by reference
/// rather than by an id, so nothing has to implement an interface and nothing has to be unregistered
/// to be collected.
/// </summary>
public sealed class ChangeTracker
{
    private readonly ConditionalWeakTable<object, SnapshotNode> _snapshots = [];

    /// <summary>Takes a snapshot, replacing any earlier one for the same object.</summary>
    public void Track<TTarget>(TTarget target) where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(target);

        _snapshots.AddOrUpdate(target, SnapshotFactory.Capture(target));
    }

    /// <summary>
    /// The changes since the snapshot was taken. Empty when nothing changed, or when the object was
    /// never tracked — use <see cref="IsTracking"/> to tell those apart.
    /// </summary>
    public IReadOnlyList<Change> GetChanges<TTarget>(TTarget target) where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(target);

        return _snapshots.TryGetValue(target, out var snapshot)
            ? SnapshotComparer.Compare(snapshot, SnapshotFactory.Capture(target))
            : [];
    }

    /// <summary>The changes since the snapshot, then a new snapshot so the next call starts from here.</summary>
    public IReadOnlyList<Change> AcceptChanges<TTarget>(TTarget target) where TTarget : class
    {
        var changes = GetChanges(target);

        if (_snapshots.TryGetValue(target, out _))
        {
            Track(target);
        }

        return changes;
    }

    /// <summary>The changes since the snapshot, then forgets the object.</summary>
    public IReadOnlyList<Change> StopTracking<TTarget>(TTarget target) where TTarget : class
    {
        var changes = GetChanges(target);
        _snapshots.Remove(target);

        return changes;
    }

    public bool IsTracking<TTarget>(TTarget target) where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(target);

        return _snapshots.TryGetValue(target, out _);
    }

    public bool HasChanges<TTarget>(TTarget target) where TTarget : class
        => GetChanges(target).Count > 0;

    /// <summary>Compares two objects directly, with nothing tracked and nothing remembered.</summary>
    public static IReadOnlyList<Change> Compare<TTarget>(TTarget original, TTarget current)
    {
        return SnapshotComparer.Compare(
            SnapshotFactory.Capture(original),
            SnapshotFactory.Capture(current)
        );
    }
}
