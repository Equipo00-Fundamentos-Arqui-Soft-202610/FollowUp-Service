using MediTrack.FollowUpService.API.Domain.Model.Aggregates;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface IMedicationComplianceRepository
{
    Task<MedicationCompliance?> FindByIdAsync(int complianceId);
    Task<ICollection<MedicationCompliance>> FindByPatientIdAsync(int patientId);
    Task<ICollection<MedicationCompliance>> FindByDoseScheduleIdAsync(int doseScheduleId);
    Task AddAsync(MedicationCompliance compliance);
    Task UpdateAsync(MedicationCompliance compliance);
    Task DeleteAsync(int complianceId);
}