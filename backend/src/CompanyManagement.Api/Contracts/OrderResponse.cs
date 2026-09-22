using CompanyManagement.Domain;

namespace CompanyManagement.Api.Contracts;

public sealed record OrderResponse(
    Guid Id,
    Guid CompanyId,
    string OrderNumber,
    decimal Amount,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    // Status travels as its string name ("Completed"), not the numeric enum value -
    // matches how it's stored in SQL Server (see CompanyManagementDbContext) and
    // means the frontend never has to know the enum's underlying int values.
    public static OrderResponse FromDomain(Order order) =>
        new(
            order.Id,
            order.CompanyId,
            order.OrderNumber,
            order.Amount,
            order.Status.ToString(),
            order.CreatedAt,
            order.UpdatedAt);
}
