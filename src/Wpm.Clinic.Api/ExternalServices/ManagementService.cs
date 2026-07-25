using Microsoft.AspNetCore.Mvc;

namespace Wpm.Clinic.ExternalServices
{
    public class ManagementService(HttpClient client)
    {
        public async Task<PetInfo> GetPetInfo(int id)
        {
            var pet_data= await client.GetFromJsonAsync< PetInfo>($"/api/pets/{id}");
            return pet_data;
        }
    }
    public class PetInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public int BreedId { get; set; }
    }
}
