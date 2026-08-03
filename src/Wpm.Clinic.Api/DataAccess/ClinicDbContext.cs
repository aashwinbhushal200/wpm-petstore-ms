using Microsoft.EntityFrameworkCore;
using Wpm.Management.Api.DataAccess;

namespace Wpm.Management.Api.DataAccess
{
    public class ClinicDbContext(DbContextOptions<ClinicDbContext> options) : DbContext(options)
    {
        public DbSet<Consultation> Consultations { get; set; }
        public DbSet<PetReplica> PetReplicas { get; set; }
        
     /*   protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Consultation>().HasData([
                new Consultation(){PatientId=1,PatientName="Doggy1", PatientAge=3,StartTime=DateTime.Today},
              
                ]
            );
        }*/
        
        

    }
}

public class PetReplica
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public record Consultation(Guid Id, int PatientId, string PatientName,int PatientAge,DateTime StartTime);
public static class ClinicDbContextExtensions
{
    public static void EnsureClinicDbCreated(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetService<ClinicDbContext>();
        context!.Database.EnsureCreated();
    }
}