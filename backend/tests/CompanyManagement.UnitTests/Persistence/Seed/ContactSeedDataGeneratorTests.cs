using CompanyManagement.Infrastructure.Persistence.Seed;
using Xunit;

namespace CompanyManagement.UnitTests.Persistence.Seed;

public class ContactSeedDataGeneratorTests
{
    [Fact]
    public void Generate_IsDeterministic_AcrossRepeatedCalls()
    {
        var companies = CompanySeedDataGenerator.Generate(200);

        var first = ContactSeedDataGenerator.Generate(companies);
        var second = ContactSeedDataGenerator.Generate(companies);

        Assert.Equal(
            first.Select(c => (c.Id, c.CompanyId, c.Email)),
            second.Select(c => (c.Id, c.CompanyId, c.Email)));
    }

    [Fact]
    public void Generate_EveryContact_HasAValidCompanyId()
    {
        var companies = CompanySeedDataGenerator.Generate(200);
        var companyIds = companies.Select(c => c.Id).ToHashSet();

        var contacts = ContactSeedDataGenerator.Generate(companies);

        Assert.All(contacts, contact => Assert.Contains(contact.CompanyId, companyIds));
    }

    [Fact]
    public void Generate_ProducesUniqueEmails_AcrossTheWholeDataset()
    {
        var companies = CompanySeedDataGenerator.Generate(500);

        var contacts = ContactSeedDataGenerator.Generate(companies);

        Assert.Equal(contacts.Count, contacts.Select(c => c.Email).Distinct().Count());
    }

    [Fact]
    public void Generate_ProducesUniqueDeterministicIds()
    {
        var companies = CompanySeedDataGenerator.Generate(500);

        var contacts = ContactSeedDataGenerator.Generate(companies);

        Assert.Equal(contacts.Count, contacts.Select(c => c.Id).Distinct().Count());
        Assert.All(contacts, contact => Assert.StartsWith("434f4e54-0000-0000-", contact.Id.ToString()));
    }

    [Fact]
    public void Generate_VariesContactCountPerCompany_IncludingSomeWithZero()
    {
        var companies = CompanySeedDataGenerator.Generate(500);

        var contacts = ContactSeedDataGenerator.Generate(companies);
        var countsByCompany = contacts.GroupBy(c => c.CompanyId).ToDictionary(g => g.Key, g => g.Count());

        Assert.True(countsByCompany.Values.Distinct().Count() > 1, "Expected a variable number of contacts per company.");
        Assert.True(companies.Count > countsByCompany.Count, "Expected at least one company with zero contacts.");
    }

    [Fact]
    public void Generate_ProducesSomeCompanies_WithOnlyInactiveContacts()
    {
        var companies = CompanySeedDataGenerator.Generate(2000);

        var contacts = ContactSeedDataGenerator.Generate(companies);
        var hasOnlyInactiveContacts = contacts
            .GroupBy(c => c.CompanyId)
            .Any(group => group.Any() && group.All(c => !c.IsActive));

        Assert.True(hasOnlyInactiveContacts, "Expected at least one company whose contacts are all inactive.");
    }
}
