namespace CompanyManagement.Domain;

public sealed class Company : IEquatable<Company>
{
    public Guid Id { get; }
    public string Name { get; }
    public string WebsiteUrl { get; }

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
