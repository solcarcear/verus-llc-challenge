using CompanyManagement.Application.Persistence;
using CompanyManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace CompanyManagement.Infrastructure.Persistence;

public sealed class EfCompanyRepository : ICompanyRepository
{
    private readonly CompanyManagementDbContext _dbContext;

    public EfCompanyRepository(CompanyManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Companies
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Company> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Companies.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        // OrderBy before Skip/Take is what makes pagination deterministic: SQL Server
        // needs a defined order to translate Skip/Take into OFFSET/FETCH at all, and
        // ThenBy(Id) breaks ties on Name so no row is ever skipped or repeated across pages.
        var items = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        // Company is immutable, so there's no tracked instance to mutate in place -
        // Update() attaches this new instance by its Id and marks every scalar
        // property as modified, producing a single full-row UPDATE statement.
        _dbContext.Companies.Update(company);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.Companies.FindAsync(new object[] { id }, cancellationToken);
        if (company is null)
        {
            return false;
        }

        _dbContext.Companies.Remove(company);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<(IReadOnlyList<CompanySummary> Items, int TotalCount)> GetPagedSummariesAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Companies.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        // Projecting straight to CompanySummary - rather than loading Company entities
        // and then a separate query per row for their counts - keeps this at one
        // round trip to SQL Server: c.Contacts.Count/c.Orders.Count compile to
        // correlated subqueries evaluated set-based for the whole page, not N+1.
        var items = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CompanySummary(c.Id, c.Name, c.WebsiteUrl, c.Contacts.Count, c.Orders.Count))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Company?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // AsSplitQuery(): Contacts and Orders are sibling collections. A single joined
        // query would return one row per (contact, order) combination - a company with
        // 10 contacts and 30 orders would come back as 300 duplicated rows that EF then
        // has to de-duplicate client-side. Splitting into two queries (one per
        // collection) avoids that multiplication entirely.
        return await _dbContext.Companies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Contacts)
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, CompanyRelationshipCounts>> GetRelationshipCountsAsync(
        IReadOnlyCollection<Guid> companyIds,
        CancellationToken cancellationToken = default)
    {
        if (companyIds.Count == 0)
        {
            return new Dictionary<Guid, CompanyRelationshipCounts>();
        }

        var contactCounts = await _dbContext.Contacts
            .AsNoTracking()
            .Where(c => companyIds.Contains(c.CompanyId))
            .GroupBy(c => c.CompanyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Count, cancellationToken);

        var orderCounts = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => companyIds.Contains(o.CompanyId))
            .GroupBy(o => o.CompanyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Count, cancellationToken);

        return companyIds.ToDictionary(
            id => id,
            id => new CompanyRelationshipCounts(
                contactCounts.GetValueOrDefault(id),
                orderCounts.GetValueOrDefault(id)));
    }
}
