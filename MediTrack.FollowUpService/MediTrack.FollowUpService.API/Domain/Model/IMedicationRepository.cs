using MediTrack.FollowUpService.API.Domain.Model.Aggregates;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface IMedicationRepository
{
    Task<ICollection<Medication>> FindByPatientIdAsync(int patientId);
    Task<Medication?> FindByIdAsync(int medicationId);
    Task AddAsync(Medication medication);
    Task UpdateAsync(Medication medication);
    Task DeleteAsync(int medicationId);
}