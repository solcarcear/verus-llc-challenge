using CompanyManagement.Domain;

namespace CompanyManagement.Application.Persistence;

public interface ICompanyRepository
{
    Task AddAsync(Company company, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Ordered by Name (the natural browse order), then by Id as a tiebreaker so
    // paging stays deterministic even when two companies share the same Name.
    Task<(IReadOnlyList<Company> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Callers are expected to have already confirmed the company exists (e.g. via
    // GetByIdAsync) before calling this - it always attaches and saves.
    Task UpdateAsync(Company company, CancellationToken cancellationToken = default);

    // Returns false instead of throwing when the id doesn't exist, so callers can
    // turn that directly into a 404 without a separate existence check.
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Same paging/ordering as GetPagedAsync, but projects straight to CompanySummary
    // (one query, with ContactCount/OrderCount computed by SQL Server per row) instead
    // of loading full Contact/Order collections for every company on the page.
    Task<(IReadOnlyList<CompanySummary> Items, int TotalCount)> GetPagedSummariesAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Returns the company with its Contacts and Orders collections populated, for the
    // company details view. Null if no company with that id exists.
    Task<Company?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    // For a specific, typically small set of ids (e.g. this page's rows, or a search
    // result set) - not the whole table. Used so search results can show the same
    // Contact/Order counts as the browse list without a per-row query.
    Task<IReadOnlyDictionary<Guid, CompanyRelationshipCounts>> GetRelationshipCountsAsync(
        IReadOnlyCollection<Guid> companyIds,
        CancellationToken cancellationToken = default);
}
