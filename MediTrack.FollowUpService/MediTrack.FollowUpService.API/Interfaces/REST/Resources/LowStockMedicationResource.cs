namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record LowStockMedicationResource(
    int MedicationId,
    string MedicationName,
    string Dose,
    int StockCount,
    string Message
);
