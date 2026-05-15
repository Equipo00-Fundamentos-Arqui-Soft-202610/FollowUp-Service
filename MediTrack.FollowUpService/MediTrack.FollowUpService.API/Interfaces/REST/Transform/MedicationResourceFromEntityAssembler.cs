using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Transform;

public class MedicationResourceFromEntityAssembler
{
    public MedicationResource ToResource(Medication medication)
    {
        return new MedicationResource(
            Id: medication.Id,
            PatientId: medication.PatientId,
            Name: medication.Name,
            Dose: medication.Dose,
            FrequencyHours: medication.FrequencyHours,
            StartDate: medication.StartDate,
            EndDate: medication.EndDate,
            StockCount: medication.StockCount,
            IsActive: medication.IsActive,
            Schedules: medication.Schedules
                .Select(s => new DoseScheduleResource(
                    Id: s.Id,
                    ScheduledTime: s.ScheduledTime,
                    IsActive: s.IsActive
                ))
                .ToList()
        );
    }

    public ICollection<MedicationResource> ToResources(ICollection<Medication> medications)
    {
        return medications
            .Select(ToResource)
            .ToList();
    }
}