using Microsoft.EntityFrameworkCore;

namespace Wpm.Billing.Api.DataAccess;

public class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>()
            .Property(i => i.Amount)
            .HasPrecision(18, 2);
    }
}

public class Invoice
{
    public Guid Id { get; set; }
    public Guid ConsultationId { get; set; }
    public int PatientId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class BillingDbContextExtensions
{
    public static void EnsureBillingDbCreated(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetService<BillingDbContext>();
        context!.Database.EnsureCreated();
    }
}
