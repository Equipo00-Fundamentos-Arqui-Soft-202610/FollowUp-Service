namespace MediTrack.FollowUpService.API.Domain.Model.Aggregates;

public class DoseSchedule
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public TimeOnly ScheduledTime { get; set; }
    public bool IsActive { get; set; }
    public Medication Medication { get; set; } = null!;

    public DoseSchedule() { }

    public DoseSchedule(int medicationId, TimeOnly scheduledTime, bool isActive = true)
    {
        MedicationId = medicationId;
        ScheduledTime = scheduledTime;
        IsActive = isActive;
    }
}