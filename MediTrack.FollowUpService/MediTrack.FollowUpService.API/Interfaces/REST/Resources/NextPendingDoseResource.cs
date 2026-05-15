namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record NextPendingDoseResource(
    string MedicationName,
    string Dose,
    string ScheduledTime, // HH:mm format
    int MinutesUntilDose
);

