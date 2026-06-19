namespace MediTrack.FollowUpService.API.Domain.Model.Aggregates;

public class AppointmentCompliance
{
    public int Id { get; private set; }
    public int PatientId { get; private set; }
    public int AppointmentId { get; private set; }
    public bool Attended { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public string? Notes { get; private set; }

    protected AppointmentCompliance() { }

    public AppointmentCompliance(int patientId, int appointmentId, bool attended, string? notes)
    {
        if (patientId <= 0) throw new ArgumentException("PatientId must be positive");
        if (appointmentId <= 0) throw new ArgumentException("AppointmentId must be positive");

        PatientId = patientId;
        AppointmentId = appointmentId;
        Attended = attended;
        Notes = notes;
        RecordedAt = DateTime.UtcNow;
    }
}
