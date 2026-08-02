namespace Wpm.Shared.Events;

public record ConsultationStartedEvent
{
    public Guid ConsultationId { get; init; }
    public int PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public int PatientAge { get; init; }
    public DateTime StartTime { get; init; }
}
