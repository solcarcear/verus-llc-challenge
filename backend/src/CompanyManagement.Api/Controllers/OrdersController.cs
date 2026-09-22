using CompanyManagement.Api.Contracts;
using CompanyManagement.Domain;
using CompanyManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompanyManagement.Api.Controllers;

// Same reasoning as ContactsController: reads directly from
// CompanyManagementDbContext rather than through a repository/service layer,
// since this is a plain filtered/paginated read with no business logic to
// orchestrate. See the comment on ContactsController for the full explanation.
[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly CompanyManagementDbContext _dbContext;

    public OrdersController(CompanyManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] Guid? companyId,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var query = _dbContext.Orders.AsNoTracking();

        if (companyId is not null)
        {
            query = query.Where(o => o.CompanyId == companyId);
        }

        // An unrecognized status value is treated the same as "no filter" rather than
        // a 400 - consistent with how invalid pageNumber/pageSize are normalized
        // instead of rejected elsewhere in this API.
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .ThenBy(o => o.Id)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(o => new OrderResponse(
                o.Id, o.CompanyId, o.OrderNumber, o.Amount, o.Status.ToString(), o.CreatedAt, o.UpdatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        return Ok(new PagedResponse<OrderResponse>(
            items, normalizedPageNumber, normalizedPageSize, totalCount, totalPages));
    }
}
