using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
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
    [HttpPost("{id}")]
    public async Task<IActionResult> Update(int id, PetUpdate petUpdate)
    {
        try
        {
            var pet = await managementDbContext.Pets.FindAsync(id);

            if (pet == null)
            {
                return NotFound(id);
            }

            pet.Name = petUpdate.Name;
            pet.Age = petUpdate.Age;
            pet.BreedId = petUpdate.BreedId;

            await managementDbContext.SaveChangesAsync();

            return Ok(petUpdate);
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
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
    public record PetUpdate(string Name, int Age, int BreedId);
}