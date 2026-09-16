using CompanyManagement.Api.ExceptionHandling;
using CompanyManagement.Application.Persistence;
using CompanyManagement.Application.Relevance;
using CompanyManagement.Application.Services;
using CompanyManagement.Application.Validation;
using CompanyManagement.Infrastructure.Persistence;

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

// Singletons: CompanyValidator and CompanyRelevanceEvaluator are stateless.
// InMemoryCompanyRepository must be a singleton so stored companies survive
// across requests instead of being reset with every per-request DI scope.
builder.Services.AddSingleton<ICompanyValidator, CompanyValidator>();
builder.Services.AddSingleton<ICompanyRelevanceEvaluator, CompanyRelevanceEvaluator>();
builder.Services.AddSingleton<ICompanyRepository, InMemoryCompanyRepository>();
builder.Services.AddScoped<ICompanyService, CompanyService>();

var app = builder.Build();

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
