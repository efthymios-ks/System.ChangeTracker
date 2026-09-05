# System.ChangeTracker

Snapshot an object, edit it, get a flat list of what changed. No base class, no interface, no
attributes. A demo, not a package — clone it and copy what is useful.

```
ChangeTracker.cs      Track / GetChanges / AcceptChanges / StopTracking / Compare
Change.cs             Path, Kind, OldValue, NewValue
ChangeKind.cs         Added, Removed, Modified
IChangeTrackable.cs   optional, gives collection items a stable identity
Snapshots/            internal capture and comparison
```

## Track and compare

```csharp
var tracker = new ChangeTracker();
tracker.Track(order);

order.Total = 25m;
order.Customer.Address.City = "Patras";

foreach (var change in tracker.GetChanges(order))
{
    Console.WriteLine(change);
}

// Total: 10 -> 25
// Customer.Address.City: Athens -> Patras
```

Objects are held by reference, so nothing needs an id and nothing needs unregistering to be
collected. Snapshots are deep copies of the values — later edits cannot reach back into them.

| Method | Does |
| --- | --- |
| `Track` | takes a snapshot, replacing any earlier one |
| `GetChanges` | changes since the snapshot; the snapshot stays |
| `AcceptChanges` | changes since the snapshot, then re-snapshots |
| `StopTracking` | changes since the snapshot, then forgets the object |
| `IsTracking` | whether a snapshot exists |
| `HasChanges` | whether `GetChanges` would return anything |

`GetChanges` on an untracked object returns nothing rather than throwing — use `IsTracking` to tell
"unchanged" from "never tracked" apart.

## Compare two objects

No tracking, nothing remembered.

```csharp
var changes = ChangeTracker.Compare(before, after);
```

## Changes

```csharp
public sealed record Change(string Path, ChangeKind Kind, object? OldValue, object? NewValue);
```

The path addresses the leaf that moved, so a change is one line to log or display.

| Path | Means |
| --- | --- |
| `Total` | a property on the root |
| `Customer.Address.City` | a property on a nested object |
| `Tags[1]` | an item matched by position |
| `Lines[SKU-9].Quantity` | a property of an item matched by `TrackId` |

## Collection identity

Without an identity, items are matched by position — inserting at the front reads as every later
slot having changed. Implement `IChangeTrackable` on the item to match by key instead.

```csharp
public sealed class OrderLine : IChangeTrackable
{
    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string TrackId => Sku;
}
```

```csharp
order.Lines.Reverse();              // no changes
order.Lines.Insert(0, newLine);     // Lines[SKU-9]: + {object}
```

Unit tests only — nothing here touches a database or a network.
