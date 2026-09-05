namespace System.ChangeTracker.Tests.Shared;

public sealed class Order
{
    public int Number { get; set; }

    public decimal Total { get; set; }

    public string? Note { get; set; }

    public OrderStatus Status { get; set; }

    public Customer? Customer { get; set; }

    public List<OrderLine> Lines { get; set; } = [];

    public List<string> Tags { get; set; } = [];
}

public sealed class Customer
{
    public string Name { get; set; } = string.Empty;

    public Address? Address { get; set; }
}

public sealed class Address
{
    public string City { get; set; } = string.Empty;

    public string? Postcode { get; set; }
}

public sealed class OrderLine : IChangeTrackable
{
    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string TrackId => Sku;
}

public sealed class PlainLine
{
    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}

public enum OrderStatus
{
    Draft = 0,
    Placed = 1,
    Shipped = 2
}

public sealed class SelfReferencing
{
    public string Name { get; set; } = string.Empty;

    public SelfReferencing? Next { get; set; }
}

public sealed class Throwing
{
    public string Safe { get; set; } = string.Empty;

    public string Explodes => throw new InvalidOperationException("no");
}
