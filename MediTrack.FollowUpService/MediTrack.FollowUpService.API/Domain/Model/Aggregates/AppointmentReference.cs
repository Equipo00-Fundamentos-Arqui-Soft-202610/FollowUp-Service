namespace MediTrack.FollowUpService.API.Domain.Model.Aggregates;

public class AppointmentReference
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public DateTime ScheduledAt { get; set; }

    public AppointmentReference() { }

    public static AppointmentReference CreateFromEvent(int id, int patientId, DateTime scheduledAt)
    {
        return new AppointmentReference { Id = id, PatientId = patientId, ScheduledAt = scheduledAt };
    }
}
