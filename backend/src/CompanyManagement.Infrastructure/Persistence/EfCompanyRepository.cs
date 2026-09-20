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
}
