using CompanyManagement.Application.Relevance;
using CompanyManagement.Application.Validation;
using CompanyManagement.Infrastructure.Persistence.Seed;
using Xunit;

namespace CompanyManagement.UnitTests.Persistence.Seed;

public class CompanySeedDataGeneratorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(300)]
    public void Generate_ReturnsExactlyTheRequestedCount(int count)
    {
        var companies = CompanySeedDataGenerator.Generate(count);

        Assert.Equal(count, companies.Count);
    }

    [Fact]
    public void Generate_IsDeterministic_AcrossRepeatedCalls()
    {
        var first = CompanySeedDataGenerator.Generate(300);
        var second = CompanySeedDataGenerator.Generate(300);

        Assert.Equal(first.Select(c => (c.Id, c.Name, c.WebsiteUrl)), second.Select(c => (c.Id, c.Name, c.WebsiteUrl)));
    }

    [Fact]
    public void Generate_SmallerCount_IsAPrefixOfLargerCount()
    {
        var small = CompanySeedDataGenerator.Generate(50);
        var large = CompanySeedDataGenerator.Generate(200);

        Assert.Equal(small.Select(c => (c.Id, c.Name, c.WebsiteUrl)), large.Take(50).Select(c => (c.Id, c.Name, c.WebsiteUrl)));
    }

    [Fact]
    public void Generate_ProducesUniqueDeterministicIds()
    {
        var companies = CompanySeedDataGenerator.Generate(1000);

        Assert.Equal(1000, companies.Select(c => c.Id).Distinct().Count());
        for (var index = 0; index < companies.Count; index++)
        {
            Assert.Equal(CompanySeedDataGenerator.SeedId(index), companies[index].Id);
            Assert.StartsWith("53454544-0000-0000-", companies[index].Id.ToString());
        }
    }

    [Fact]
    public void Generate_AllRecords_SatisfyCompanyValidator()
    {
        var validator = new CompanyValidator();
        var companies = CompanySeedDataGenerator.Generate(1000);

        foreach (var company in companies)
        {
            var result = validator.Validate(company.Name, company.WebsiteUrl);
            Assert.True(result.IsValid, $"Expected valid company but got errors for '{company.Name}' / '{company.WebsiteUrl}': {string.Join(", ", result.Errors)}");
        }
    }

    [Fact]
    public void Generate_ProducesAllRelevanceOutcomes()
    {
        var evaluator = new CompanyRelevanceEvaluator();
        var companies = CompanySeedDataGenerator.Generate(2000);

        var scores = companies
            .Select(c => evaluator.Evaluate(c.Name, c.WebsiteUrl))
            .ToList();

        Assert.Contains(scores, r => r.IsRelevant && r.Score == 100);
        Assert.Contains(scores, r => r.IsRelevant && r.Score == 80);
        Assert.Contains(scores, r => r.IsRelevant && r.Score == 60);
        Assert.Contains(scores, r => !r.IsRelevant);
    }

    [Fact]
    public void Generate_DoesNotProduceNearlyIdenticalNames()
    {
        var companies = CompanySeedDataGenerator.Generate(2000);

        var distinctNames = companies.Select(c => c.Name).Distinct().Count();

        Assert.True(distinctNames > companies.Count / 2, "Expected substantial name variation across generated records.");
    }
}
