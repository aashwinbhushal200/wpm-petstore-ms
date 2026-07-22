using Microsoft.EntityFrameworkCore;
using Wpm.Management.Api.DataAccess;

namespace Wpm.Management.Api.DataAccess
{
    public class ManagementDbContext(DbContextOptions<ManagementDbContext> options) : DbContext(options)
    {
        public DbSet<Pet> Pets { get; set; }
        public DbSet<Breed> Breeds { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Breed>().HasData([
                new Breed(1,"Beagle"),
                new Breed(2,"Golden R")]
            );
            modelBuilder.Entity<Pet>().HasData([
                new Pet(){Id=1,Name="kali1", Age=3,BreedId=1},
                new Pet(){Id=2,Name="kali2", Age=5,BreedId=2}]
            );
        }
        
        

    }
}
public class Pet
{
    public int Id { get; set; }
    public int Age { get; set; }
    public int BreedId { get; set; }
    public string Name { get; set; }
    public Breed Breed { get; set; }

}

public record Breed(int Id, string name);
public static class ManagementDbContextExtensions
{
    public static void EnsureDbCreated(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetService<ManagementDbContext>();
        context!.Database.EnsureCreated();
    }
}