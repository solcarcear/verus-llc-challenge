namespace CompanyManagement.Application.Relevance;

public sealed record CompanyRelevanceResult(bool IsRelevant, int Score)
{
    private const int RelevanceThreshold = 60;

    public static CompanyRelevanceResult Relevant(int score) => new(score >= RelevanceThreshold, score);

    public static CompanyRelevanceResult NotRelevant() => new(false, 0);
}
