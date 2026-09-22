namespace CompanyManagement.Domain;

public sealed class Order : IEquatable<Order>
{
    public Guid Id { get; }
    public Guid CompanyId { get; }
    public string OrderNumber { get; }
    public decimal Amount { get; }
    public OrderStatus Status { get; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; }

    // Populated by EF Core when a query uses Include(o => o.Company). See the
    // same note on Contact.Company - this is the one mutable member here.
    public Company Company { get; private set; } = null!;

    public Order(
        Guid id,
        Guid companyId,
        string orderNumber,
        decimal amount,
        OrderStatus status,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        Id = id;
        CompanyId = companyId;
        OrderNumber = orderNumber;
        Amount = amount;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public bool Equals(Order? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Order);

    public override int GetHashCode() => Id.GetHashCode();
}
