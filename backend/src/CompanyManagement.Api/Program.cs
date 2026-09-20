using CompanyManagement.Api.ExceptionHandling;
using CompanyManagement.Application.Persistence;
using CompanyManagement.Application.Relevance;
using CompanyManagement.Application.Services;
using CompanyManagement.Application.Validation;
using CompanyManagement.Infrastructure.Persistence;
using CompanyManagement.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

const string AngularDevelopmentCorsPolicy = "AngularDevelopment";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Local development only: allows the Angular dev server (ng serve, default
// port 4200) to call this API. Not intended for production origins.
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevelopmentCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<CompanyManagementDbContext>(options =>
{
    // TestWebApplicationFactory fully replaces this registration with the EF Core InMemory
    // provider before it's ever resolved, so no real connection string is required when
    // running under the "Testing" environment.
    if (builder.Environment.IsEnvironment("Testing"))
    {
        return;
    }

    var sqlConnectionString = builder.Configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Connection string 'SqlServer' is not configured.");

    options.UseSqlServer(sqlConnectionString);
});

// Singletons: CompanyValidator and CompanyRelevanceEvaluator are stateless.
builder.Services.AddSingleton<ICompanyValidator, CompanyValidator>();
builder.Services.AddSingleton<ICompanyRelevanceEvaluator, CompanyRelevanceEvaluator>();

// Scoped: EfCompanyRepository depends on the per-request CompanyManagementDbContext.
builder.Services.AddScoped<ICompanyRepository, EfCompanyRepository>();
builder.Services.AddScoped<ICompanyService, CompanyService>();

builder.Services.Configure<SeedDataOptions>(builder.Configuration.GetSection(SeedDataOptions.SectionName));
builder.Services.AddScoped<CompanyDevelopmentSeeder>();

var app = builder.Build();

// Skipped under the "Testing" environment: integration tests swap in the EF Core
// InMemory provider, which does not support relational migrations.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<CompanyManagementDbContext>();
    dbContext.Database.Migrate();
}

// Development-only: appsettings.json (used by every environment, including
// Production) has no "SeedData" section, so SeedDataOptions.Enabled defaults
// to false there. The IsDevelopment() check is a second, code-level gate that
// holds even if a "SeedData" section is ever added elsewhere by mistake.
if (app.Environment.IsDevelopment())
{
    using var seedScope = app.Services.CreateScope();
    var seedOptions = seedScope.ServiceProvider.GetRequiredService<IOptions<SeedDataOptions>>().Value;
    if (seedOptions.Enabled)
    {
        var seeder = seedScope.ServiceProvider.GetRequiredService<CompanyDevelopmentSeeder>();
        await seeder.SeedAsync(seedOptions.CompanyCount);
    }
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(AngularDevelopmentCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
