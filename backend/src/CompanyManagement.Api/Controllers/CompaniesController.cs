using CompanyManagement.Api.Contracts;
using CompanyManagement.Application.Persistence;
using CompanyManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompanyManagement.Api.Controllers;

[ApiController]
[Route("api/companies")]
public sealed class CompaniesController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly ICompanyService _companyService;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(ICompanyService companyService, ILogger<CompaniesController> logger)
    {
        _companyService = companyService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCompany(
        [FromBody] CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _companyService.CreateCompanyAsync(request.Name, request.WebsiteUrl, cancellationToken);

        switch (result.Status)
        {
            case CompanyCreationStatus.Created:
                var response = CompanyResponse.FromDomain(result.Company!);
                _logger.LogInformation("Created company {CompanyId}", response.Id);
                return CreatedAtAction(nameof(GetCompanyById), new { id = response.Id }, response);

            case CompanyCreationStatus.ValidationFailed:
                _logger.LogWarning("Company creation rejected due to validation errors");
                return BadRequest(new ApiErrorResponse("Company validation failed.", result.Errors));

            case CompanyCreationStatus.NotRelevant:
                _logger.LogWarning("Company creation rejected because the company is not relevant to the website");
                return BadRequest(new ApiErrorResponse("Company is not relevant to the provided website.", result.Errors));

            default:
                throw new InvalidOperationException($"Unhandled company creation status: {result.Status}");
        }
    }

    // Search returns its full relevance-scored result set unpaginated (it's already a
    // whole-dataset, in-memory ranking operation over a typically small match count).
    // Plain browsing (no search) is paginated, since that's the path that would otherwise
    // load the entire Companies table into memory. The browse list returns
    // CompanyListItemResponse (with Contact/Order counts) rather than plain CompanyResponse,
    // since the UI's main use of this endpoint is showing those counts per row.
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CompanyListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCompanies(
        [FromQuery(Name = "search")] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchResults = await _companyService.SearchCompaniesAsync(search, cancellationToken);
            var counts = await _companyService.GetRelationshipCountsAsync(
                searchResults.Select(c => c.Id).ToList(), cancellationToken);

            return Ok(searchResults.Select(c =>
            {
                var relationshipCounts = counts.GetValueOrDefault(c.Id, new CompanyRelationshipCounts(0, 0));
                return new CompanyListItemResponse(
                    c.Id, c.Name, c.WebsiteUrl, relationshipCounts.ContactCount, relationshipCounts.OrderCount);
            }));
        }

        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var (companies, totalCount) = await _companyService.GetCompanySummariesPagedAsync(
            normalizedPageNumber, normalizedPageSize, cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        return Ok(new PagedResponse<CompanyListItemResponse>(
            companies.Select(CompanyListItemResponse.FromSummary).ToList(),
            normalizedPageNumber,
            normalizedPageSize,
            totalCount,
            totalPages));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyById(Guid id, CancellationToken cancellationToken)
    {
        var company = await _companyService.GetCompanyByIdAsync(id, cancellationToken);

        if (company is null)
        {
            return NotFound();
        }

        return Ok(CompanyResponse.FromDomain(company));
    }

    // Separate from GetCompanyById on purpose: that endpoint is the lightweight one used
    // by CreatedAtAction/edit flows, while this one does the heavier Contacts/Orders
    // Include for the company details screen - callers that don't need the relationships
    // (like the edit modal) aren't paying for them.
    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(CompanyDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyDetails(Guid id, CancellationToken cancellationToken)
    {
        var company = await _companyService.GetCompanyDetailsAsync(id, cancellationToken);

        if (company is null)
        {
            return NotFound();
        }

        return Ok(CompanyDetailsResponse.FromDomain(company));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCompany(
        Guid id,
        [FromBody] UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _companyService.UpdateCompanyAsync(id, request.Name, request.WebsiteUrl, cancellationToken);

        switch (result.Status)
        {
            case CompanyUpdateStatus.Updated:
                _logger.LogInformation("Updated company {CompanyId}", id);
                return Ok(CompanyResponse.FromDomain(result.Company!));

            case CompanyUpdateStatus.NotFound:
                return NotFound();

            case CompanyUpdateStatus.ValidationFailed:
                _logger.LogWarning("Company update rejected due to validation errors");
                return BadRequest(new ApiErrorResponse("Company validation failed.", result.Errors));

            case CompanyUpdateStatus.NotRelevant:
                _logger.LogWarning("Company update rejected because the company is not relevant to the website");
                return BadRequest(new ApiErrorResponse("Company is not relevant to the provided website.", result.Errors));

            default:
                throw new InvalidOperationException($"Unhandled company update status: {result.Status}");
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCompany(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _companyService.DeleteCompanyAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        _logger.LogInformation("Deleted company {CompanyId}", id);
        return NoContent();
    }
}
