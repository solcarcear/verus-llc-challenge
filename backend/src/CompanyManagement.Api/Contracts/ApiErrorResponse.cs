namespace CompanyManagement.Api.Contracts;

public sealed record ApiErrorResponse(string Message, IReadOnlyList<string> Errors);
