namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record NextPendingDoseResource(
    int DoseScheduleId,
    string MedicationName,
    string Dose,
    string ScheduledTime,
    int MinutesUntilDose
);