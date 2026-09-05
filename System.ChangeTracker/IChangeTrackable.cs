namespace System.ChangeTracker;

/// <summary>
/// Gives a collection item a stable identity. Without it, items are matched by position, so
/// inserting one at the front reads as every later item having changed.
/// </summary>
public interface IChangeTrackable
{
    string TrackId { get; }
}
