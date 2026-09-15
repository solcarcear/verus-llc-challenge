using CompanyManagement.Domain;
using Xunit;

namespace CompanyManagement.UnitTests.Domain;

public class CompanyTests
{
    [Fact]
    public void Equals_ReturnsTrue_WhenIdsMatch_EvenIfOtherPropertiesDiffer()
    {
        var id = Guid.NewGuid();
        var first = new Company(id, "Acme Corp", "https://acme.com");
        var second = new Company(id, "Different Name", "https://different.com");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenIdsDiffer_EvenIfOtherPropertiesMatch()
    {
        var first = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");
        var second = new Company(Guid.NewGuid(), "Acme Corp", "https://acme.com");

        Assert.NotEqual(first, second);
    }
}
