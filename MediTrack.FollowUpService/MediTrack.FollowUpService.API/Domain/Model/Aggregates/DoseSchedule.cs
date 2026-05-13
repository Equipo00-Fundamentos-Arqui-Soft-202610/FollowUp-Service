using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.ValueObjects;

namespace MediTrack.FollowUpService.API.Domain.Models;

public class DoseSchedule
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public ScheduledHour ScheduledTime { get; set; } = null!;
    public bool IsActive { get; set; }
    public Medication Medication { get; set; } = null!;

    public DoseSchedule() { }

    public DoseSchedule(int medicationId, TimeSpan scheduledTime, bool isActive = true)
    {
        MedicationId = medicationId;
        ScheduledTime = new ScheduledHour(scheduledTime);
        IsActive = isActive;
    }
}