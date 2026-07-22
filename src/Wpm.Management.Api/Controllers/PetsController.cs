using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wpm.Management.Api.DataAccess;

namespace Wpm.Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController(ManagementDbContext managementDbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var all =await  managementDbContext.Pets.Include(p=>p.Breed).ToListAsync();
        return Ok(all);
    }
    [HttpGet("{id}", Name=nameof(GetById))]
    public async Task<IActionResult> GetById(int id)
    {
        var all = await managementDbContext.Pets.Include(p => p.Breed).Where(p=>p.Id==id).FirstOrDefaultAsync();
        return Ok(all);
    }
    [HttpPost()]
    public async Task<IActionResult> Create(NewPet newPet)
    {
        var NewPet = newPet.ToPet();
        await managementDbContext.Pets.AddAsync(NewPet);
        await managementDbContext.SaveChangesAsync();

        return CreatedAtRoute(nameof(GetById), new { id = NewPet.Id }, NewPet);
    }

    public record NewPet(string Name, int Age, int BreedId)
    {
        public Pet ToPet() => new Pet
        {
            Name = Name,
            Age = Age,
            BreedId = BreedId
        };
    }
}