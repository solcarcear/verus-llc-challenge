namespace CompanyManagement.Domain;

public sealed class Company : IEquatable<Company>
{
    public Guid Id { get; }
    public string Name { get; }
    public string WebsiteUrl { get; }

    // Get-only collections backed by a real list: EF Core populates them in
    // place (via Include/reflection) rather than replacing the property, so
    // these stay consistent with the rest of Company's immutability even
    // though nothing outside EF ever assigns to them directly.
    public ICollection<Contact> Contacts { get; } = new List<Contact>();
    public ICollection<Order> Orders { get; } = new List<Order>();

    public Company(Guid id, string name, string websiteUrl)
    {
        Id = id;
        Name = name;
        WebsiteUrl = websiteUrl;
    }

    public bool Equals(Company? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Company);

    public override int GetHashCode() => Id.GetHashCode();
}
