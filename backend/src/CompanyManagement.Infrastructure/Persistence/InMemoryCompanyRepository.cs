using System.Collections.Concurrent;
using CompanyManagement.Application.Persistence;
using CompanyManagement.Domain;

namespace CompanyManagement.Infrastructure.Persistence;

public sealed class InMemoryCompanyRepository : ICompanyRepository
{
    private readonly ConcurrentDictionary<Guid, Company> _companies = new();

    public Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _companies[company.Id] = company;

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Company> snapshot = _companies.Values.ToList();

        return Task.FromResult(snapshot);
    }

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _companies.TryGetValue(id, out var company);

        return Task.FromResult(company);
    }

    public Task<(IReadOnlyList<Company> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ordered = _companies.Values
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ThenBy(c => c.Id)
            .ToList();

        IReadOnlyList<Company> page = ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((page, ordered.Count));
    }

    public Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _companies[company.Id] = company;

        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_companies.TryRemove(id, out _));
    }

    public Task<(IReadOnlyList<CompanySummary> Items, int TotalCount)> GetPagedSummariesAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ordered = _companies.Values
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ThenBy(c => c.Id)
            .ToList();

        // Counts come straight from each Company's own Contacts/Orders collections -
        // there's no separate in-memory Contact/Order store, so these are always 0
        // here. That's accurate, not a special case: nothing in this implementation
        // ever adds a Contact or Order to a Company's collection in the first place.
        IReadOnlyList<CompanySummary> page = ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CompanySummary(c.Id, c.Name, c.WebsiteUrl, c.Contacts.Count, c.Orders.Count))
            .ToList();

        return Task.FromResult((page, ordered.Count));
    }

    public Task<Company?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _companies.TryGetValue(id, out var company);

        return Task.FromResult(company);
    }

    public Task<IReadOnlyDictionary<Guid, CompanyRelationshipCounts>> GetRelationshipCountsAsync(
        IReadOnlyCollection<Guid> companyIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<Guid, CompanyRelationshipCounts> result = companyIds.ToDictionary(
            id => id,
            id => _companies.TryGetValue(id, out var company)
                ? new CompanyRelationshipCounts(company.Contacts.Count, company.Orders.Count)
                : new CompanyRelationshipCounts(0, 0));

        return Task.FromResult(result);
    }
}
