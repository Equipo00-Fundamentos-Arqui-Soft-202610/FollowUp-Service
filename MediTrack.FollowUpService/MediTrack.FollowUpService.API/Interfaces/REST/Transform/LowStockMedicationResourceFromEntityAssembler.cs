using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Transform;

public class LowStockMedicationResourceFromEntityAssembler
{
    public LowStockMedicationResource ToResource(Medication medication)
    {
        return new LowStockMedicationResource(
            MedicationId: medication.Id,
            MedicationName: medication.Name,
            Dose: medication.Dose.Value,
            StockCount: medication.StockCount,
            Message: $"Te quedan {medication.StockCount} pastillas de {medication.Name}"
        );
    }

    public ICollection<LowStockMedicationResource> ToResources(ICollection<Medication> medications)
    {
        return medications
            .Select(ToResource)
            .ToList();
    }
}
