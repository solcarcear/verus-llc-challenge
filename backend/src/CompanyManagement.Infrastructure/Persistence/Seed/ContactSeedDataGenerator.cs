using CompanyManagement.Domain;

namespace CompanyManagement.Infrastructure.Persistence.Seed;

// Deterministic, like CompanySeedDataGenerator: Generate(companies) always
// produces the same contacts for the same input companies, because each
// company's contact count and each contact's fields are derived from the
// company's own index rather than a shared RNG stream.
//
// Contact counts per company are randomized between 0 and
// MaxContactsPerCompany (inclusive), so a meaningful fraction of companies
// end up with zero contacts - intentional, for LEFT JOIN / NOT EXISTS
// practice. IsActive is independently randomized per contact, so companies
// with only a couple of contacts will sometimes end up with none of them
// active, again without any special-cased logic.
public static class ContactSeedDataGenerator
{
    // ASCII "CONT" as the Guid's first component, mirroring
    // CompanySeedDataGenerator's "SEED" marker, so seeded contacts are
    // identifiable by Id prefix alone.
    private const int SeedIdMagic = 0x434F4E54;

    private const int MinContactsPerCompany = 0;
    private const int MaxContactsPerCompany = 10;
    private const int ActiveChancePercent = 80;
    private const int PhoneChancePercent = 70;
    private const int LookbackDays = 730;

    private static readonly string[] FirstNames =
    {
        "James", "Mary", "Robert", "Patricia", "John", "Jennifer", "Michael", "Linda", "David", "Elizabeth",
        "William", "Barbara", "Richard", "Susan", "Joseph", "Jessica", "Thomas", "Sarah", "Charles", "Karen",
        "Daniel", "Nancy", "Matthew", "Lisa", "Anthony", "Betty", "Mark", "Margaret", "Donald", "Sandra",
        "Steven", "Ashley", "Paul", "Kimberly", "Andrew", "Emily", "Joshua", "Donna", "Kenneth", "Michelle",
    };

    private static readonly string[] LastNames =
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
        "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
        "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson",
        "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
    };

    private static readonly string[] JobTitles =
    {
        "Procurement Manager", "Operations Director", "Finance Analyst", "Account Executive", "IT Manager",
        "Office Manager", "VP of Sales", "Purchasing Agent", "Controller", "Logistics Coordinator",
        "Customer Success Manager", "Business Owner", "Supply Chain Manager", "Admin Assistant", "CFO",
    };

    public static Guid SeedId(int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new Guid(SeedIdMagic, 0, 0, BitConverter.GetBytes((long)index));
    }

    public static IReadOnlyList<Contact> Generate(IReadOnlyList<Company> companies)
    {
        var contacts = new List<Contact>();
        var now = DateTime.UtcNow;
        var globalIndex = 0;

        for (var companyIndex = 0; companyIndex < companies.Count; companyIndex++)
        {
            var company = companies[companyIndex];
            var rng = new Random(unchecked(SeedIdMagic + (companyIndex * 104729)));
            var contactCount = rng.Next(MinContactsPerCompany, MaxContactsPerCompany + 1);

            for (var i = 0; i < contactCount; i++)
            {
                var firstName = FirstNames[rng.Next(FirstNames.Length)];
                var lastName = LastNames[rng.Next(LastNames.Length)];
                var isActive = rng.Next(100) < ActiveChancePercent;
                var jobTitle = JobTitles[rng.Next(JobTitles.Length)];
                var phone = rng.Next(100) < PhoneChancePercent ? BuildPhone(rng) : null;
                var createdAt = now.AddDays(-rng.Next(0, LookbackDays)).AddSeconds(-rng.Next(0, 86_400));

                // A strictly incrementing counter, not random text, so Email stays
                // globally unique across the whole seeded dataset without needing
                // a collision check - required since Contacts.Email is unique.
                var email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}{globalIndex}@example.test";

                contacts.Add(new Contact(
                    SeedId(globalIndex),
                    company.Id,
                    firstName,
                    lastName,
                    email,
                    phone,
                    jobTitle,
                    isActive,
                    createdAt));

                globalIndex++;
            }
        }

        return contacts;
    }

    private static string BuildPhone(Random rng) =>
        $"+1-{rng.Next(200, 999)}-{rng.Next(200, 999)}-{rng.Next(1000, 9999)}";
}
