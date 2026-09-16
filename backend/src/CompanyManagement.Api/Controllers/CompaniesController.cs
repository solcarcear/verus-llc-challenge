using CompanyManagement.Api.Contracts;
using CompanyManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompanyManagement.Api.Controllers;

[ApiController]
[Route("api/companies")]
public sealed class CompaniesController : ControllerBase
{
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

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CompanyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CompanyResponse>>> GetAllCompanies(CancellationToken cancellationToken)
    {
        var companies = await _companyService.GetAllCompaniesAsync(cancellationToken);
        return Ok(companies.Select(CompanyResponse.FromDomain));
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
}
