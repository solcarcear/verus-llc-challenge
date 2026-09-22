namespace CompanyManagement.Domain;

public sealed class Contact : IEquatable<Contact>
{
    public Guid Id { get; }
    public Guid CompanyId { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Email { get; }
    public string? Phone { get; }
    public string? JobTitle { get; }
    public bool IsActive { get; }
    public DateTime CreatedAt { get; }

    // Populated by EF Core when a query uses Include(c => c.Company). The
    // scalar CompanyId above is the real, always-present foreign key; this
    // reference is the only mutable member on an otherwise immutable entity.
    public Company Company { get; private set; } = null!;

    public Contact(
        Guid id,
        Guid companyId,
        string firstName,
        string lastName,
        string email,
        string? phone,
        string? jobTitle,
        bool isActive,
        DateTime createdAt)
    {
        Id = id;
        CompanyId = companyId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        JobTitle = jobTitle;
        IsActive = isActive;
        CreatedAt = createdAt;
    }

    public bool Equals(Contact? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Contact);

    public override int GetHashCode() => Id.GetHashCode();
}
