using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wpm.Management.Api.DataAccess;

namespace Wpm.Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BreedsController(ManagementDbContext managementDbContext,ILogger<BreedsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var all =await  managementDbContext.Breeds.ToListAsync();
        return all==null?NotFound():Ok(all);
        
    }
    [HttpGet("{id}", Name=nameof(GetBreedById))]
    public async Task<IActionResult> GetBreedById(int id)
    {
        var all = await managementDbContext.Breeds.Where(p=>p.Id==id).FirstOrDefaultAsync();
        return all == null ? NotFound() : Ok(all);
    }
    [HttpPost()]
    public async Task<IActionResult> Create(NewBreed newBreed)
    {
        try
        {
            var NewBreed = newBreed.ToBreed();
            await managementDbContext.Breeds.AddAsync(NewBreed);
            await managementDbContext.SaveChangesAsync();

            return CreatedAtRoute(nameof(GetBreedById), new { id = NewBreed.Id }, NewBreed);
        }

        catch (Exception ex)
        {
            // Log the exception (ex) here if needed
            logger.LogError(ex.ToString());// "An error occurred while creating a new breed.");
            return StatusCode(500, "An error occurred while checking for existing breeds.");
        }
     }
    public record NewBreed(string Name)
    {
        public Breed ToBreed() 
        {
            return new Breed
            (
                Id: 0,
                name: Name
            );
        }
    }
}