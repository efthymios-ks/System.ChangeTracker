using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace System.ChangeTracker.Snapshots;

/// <summary>Walks an object graph once and copies out everything worth comparing later.</summary>
internal static class SnapshotFactory
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ReadableProperties = new();

    /// <summary>Types compared by value even though they are neither primitive nor sealed structs.</summary>
    private static readonly HashSet<Type> ValueTypes =
    [
        typeof(string),
        typeof(decimal),
        typeof(Guid),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(Uri),
        typeof(Version)
    ];

    public static SnapshotNode Capture(object? target)
        => Capture(target, new HashSet<object>(ReferenceEqualityComparer.Instance));

    private static SnapshotNode Capture(object? target, HashSet<object> branch)
    {
        if (target is null || IsValue(target.GetType()))
        {
            return new ValueNode(target);
        }

        // Only the current branch is guarded: the same object appearing twice side by side is
        // legitimate, whereas meeting it again on the way down is a cycle.
        if (!branch.Add(target))
        {
            return CycleNode.Instance;
        }

        try
        {
            return target is IEnumerable enumerable
                ? CaptureCollection(enumerable, branch)
                : CaptureObject(target, branch);
        }
        finally
        {
            branch.Remove(target);
        }
    }

    private static SnapshotNode CaptureObject(object target, HashSet<object> branch)
    {
        var properties = new Dictionary<string, SnapshotNode>(StringComparer.Ordinal);

        foreach (var property in ReadablePropertiesOf(target.GetType()))
        {
            properties[property.Name] = Capture(ReadOrNull(property, target), branch);
        }

        return new ObjectNode(properties);
    }

    private static SnapshotNode CaptureCollection(IEnumerable target, HashSet<object> branch)
    {
        var items = new List<SnapshotNode>();
        var keys = new List<string?>();

        foreach (var item in target)
        {
            items.Add(Capture(item, branch));
            keys.Add((item as IChangeTrackable)?.TrackId);
        }

        return new CollectionNode(items, keys);
    }

    /// <summary>A property that throws on read is not a change; capture it as absent instead.</summary>
    private static object? ReadOrNull(PropertyInfo property, object target)
    {
        try
        {
            return property.GetValue(target);
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }

    private static PropertyInfo[] ReadablePropertiesOf(Type type)
        => ReadableProperties.GetOrAdd(type, static key =>
            [
                .. key
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
            ]);

    private static bool IsValue(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying.IsPrimitive
            || underlying.IsEnum
            || ValueTypes.Contains(underlying);
    }
}
