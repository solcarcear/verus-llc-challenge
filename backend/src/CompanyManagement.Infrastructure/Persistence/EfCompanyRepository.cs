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
}
