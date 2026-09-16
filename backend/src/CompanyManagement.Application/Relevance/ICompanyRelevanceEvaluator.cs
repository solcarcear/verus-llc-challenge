namespace CompanyManagement.Application.Relevance;

public interface ICompanyRelevanceEvaluator
{
    CompanyRelevanceResult Evaluate(string? companyName, string? websiteUrl);
}
