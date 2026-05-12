namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record DoseScheduleResource(
    int Id,
    TimeSpan ScheduledTime,
    bool IsActive
);

public record MedicationResource(
    int Id,
    int PatientId,
    string Name,
    string Dose,
    int FrequencyHours,
    DateTime StartDate,
    DateTime? EndDate,
    int StockCount,
    bool IsActive,
    ICollection<DoseScheduleResource> Schedules
);