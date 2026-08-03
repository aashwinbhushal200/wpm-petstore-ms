namespace Wpm.Shared.Events
{
    public class PetUpdatedEvent
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public int BreedId { get; set; }
    }
}
