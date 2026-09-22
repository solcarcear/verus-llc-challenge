using CompanyManagement.Api.Contracts;
using CompanyManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompanyManagement.Api.Controllers;

// Reads directly from CompanyManagementDbContext instead of going through
// ICompanyRepository/ICompanyService, unlike CompaniesController. That's a
// deliberate difference, not an inconsistency: CompanyService exists to
// orchestrate real business logic (validation, relevance, create/update/delete
// rules) around Company. Contacts have none of that yet - this is a plain,
// filtered, paginated read - so adding an IContactRepository/ContactService
// that would do nothing but forward to EF Core would be ceremony with no
// payoff. If Contact ever grows real business rules, this is exactly the
// point where it should get the same layering Company has.
[ApiController]
[Route("api/contacts")]
public sealed class ContactsController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly CompanyManagementDbContext _dbContext;

    public ContactsController(CompanyManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ContactResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContacts(
        [FromQuery] Guid? companyId,
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var query = _dbContext.Contacts.AsNoTracking();

        if (companyId is not null)
        {
            query = query.Where(c => c.CompanyId == companyId);
        }

        if (isActive is not null)
        {
            query = query.Where(c => c.IsActive == isActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Projects directly to ContactResponse inside the query (not via
        // ContactResponse.FromDomain after the fact) so EF Core can translate the
        // whole thing to SQL and select only these columns - calling a C# method
        // from inside .Select() can't be translated and would throw at runtime.
        var items = await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ThenBy(c => c.Id)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(c => new ContactResponse(
                c.Id, c.CompanyId, c.FirstName, c.LastName, c.Email, c.Phone, c.JobTitle, c.IsActive, c.CreatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        return Ok(new PagedResponse<ContactResponse>(
            items, normalizedPageNumber, normalizedPageSize, totalCount, totalPages));
    }
}
