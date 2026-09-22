using CompanyManagement.Domain;

namespace CompanyManagement.Api.Contracts;

public sealed record ContactResponse(
    Guid Id,
    Guid CompanyId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? JobTitle,
    bool IsActive,
    DateTime CreatedAt)
{
    public static ContactResponse FromDomain(Contact contact) =>
        new(
            contact.Id,
            contact.CompanyId,
            contact.FirstName,
            contact.LastName,
            contact.Email,
            contact.Phone,
            contact.JobTitle,
            contact.IsActive,
            contact.CreatedAt);
}
