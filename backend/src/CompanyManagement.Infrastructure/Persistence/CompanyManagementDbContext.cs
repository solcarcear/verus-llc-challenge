using CompanyManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace CompanyManagement.Infrastructure.Persistence;

public sealed class CompanyManagementDbContext : DbContext
{
    public CompanyManagementDbContext(DbContextOptions<CompanyManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(company =>
        {
            company.ToTable("Companies");
            company.HasKey(c => c.Id);
            company.Property(c => c.Name).IsRequired().HasMaxLength(200);
            company.Property(c => c.WebsiteUrl).IsRequired().HasMaxLength(2048);

            // Matches the OrderBy(Name).ThenBy(Id) used for paginated listing, so SQL
            // Server can satisfy that ORDER BY directly from the index instead of
            // sorting the whole table on every page request.
            company.HasIndex(c => new { c.Name, c.Id }).HasDatabaseName("IX_Companies_Name_Id");
        });

        modelBuilder.Entity<Contact>(contact =>
        {
            contact.ToTable("Contacts");
            contact.HasKey(c => c.Id);
            contact.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
            contact.Property(c => c.LastName).IsRequired().HasMaxLength(100);
            contact.Property(c => c.Email).IsRequired().HasMaxLength(256);
            contact.Property(c => c.Phone).HasMaxLength(30);
            contact.Property(c => c.JobTitle).HasMaxLength(150);
            contact.Property(c => c.IsActive).IsRequired();
            // Explicitly mapped (not just left to convention): it's otherwise the
            // only scalar property never referenced anywhere in this configuration,
            // and EF Core's constructor-binding convention needs at least one
            // reference - via Property() or HasIndex() - to recognize a property on
            // a constructor-only entity. Without this, EF throws at startup ("No
            // suitable constructor was found... Cannot bind 'createdAt'").
            contact.Property(c => c.CreatedAt).IsRequired();

            contact.HasOne(c => c.Company)
                .WithMany(co => co.Contacts)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // CompanyId is the left-most column, so this index also serves plain
            // "contacts for this company" queries - a separate standalone
            // CompanyId index would be redundant on top of this one.
            contact.HasIndex(c => new { c.CompanyId, c.IsActive })
                .HasDatabaseName("IX_Contacts_CompanyId_IsActive");

            // Enforces one contact per email address. Trade-off: every insert/update
            // pays a uniqueness check, and this assumes email should be unique
            // system-wide rather than per company - a real CRM might prefer a
            // composite unique (CompanyId, Email) if the same person could
            // legitimately be a contact at more than one company.
            contact.HasIndex(c => c.Email)
                .IsUnique()
                .HasDatabaseName("IX_Contacts_Email");
        });

        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("Orders");
            order.HasKey(o => o.Id);
            order.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
            order.Property(o => o.Amount).HasPrecision(18, 2);

            // Stored as text (e.g. "Completed") rather than the enum's underlying
            // int, since this branch exists to practice hand-written SQL against
            // this data - WHERE Status = 'Completed' reads far better in SSMS
            // than WHERE Status = 2.
            order.Property(o => o.Status).IsRequired().HasMaxLength(20).HasConversion<string>();
            order.Property(o => o.CreatedAt).IsRequired();
            // Same reasoning as Contact.CreatedAt above: UpdatedAt is otherwise
            // never referenced anywhere in this configuration, which is enough to
            // make EF Core's constructor-binding convention fail on this
            // constructor-only entity.
            order.Property(o => o.UpdatedAt);

            order.HasOne(o => o.Company)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Covers "orders for this company" (left-most prefix) and "latest
            // orders for this company" (full key, already sorted by CreatedAt) -
            // a separate standalone CompanyId index would be redundant.
            order.HasIndex(o => new { o.CompanyId, o.CreatedAt })
                .HasDatabaseName("IX_Orders_CompanyId_CreatedAt");

            // For "recent orders in a given status" (e.g. pending/completed),
            // independent of which company they belong to.
            order.HasIndex(o => new { o.Status, o.CreatedAt })
                .HasDatabaseName("IX_Orders_Status_CreatedAt");

            // OrderNumber is a business identifier (like an invoice number) -
            // it should never collide, same reasoning as Contact.Email.
            order.HasIndex(o => o.OrderNumber)
                .IsUnique()
                .HasDatabaseName("IX_Orders_OrderNumber");
        });
    }
}
