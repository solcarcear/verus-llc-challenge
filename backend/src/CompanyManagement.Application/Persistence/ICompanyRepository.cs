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
}
