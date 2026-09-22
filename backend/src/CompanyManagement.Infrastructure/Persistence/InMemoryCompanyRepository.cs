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
}
