using MediTrack.FollowUpService.API.Domain.Model.Aggregates;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface IMedicationRepository
{
    Task<ICollection<Medication>> FindByPatientIdAsync(int patientId);
    Task<Medication?> FindByIdAsync(int medicationId);

    /// Medicamentos activos (`EndDate` nulo o futuro) de TODOS los pacientes,
    /// con sus horarios (`Schedules`) cargados — usado por
    /// `StaleDoseExpirationService` para recorrer todas las ocurrencias de
    /// dosis del día sin depender de un `patientId` concreto.
    Task<ICollection<Medication>> FindAllActiveAsync();

    Task AddAsync(Medication medication);
    Task UpdateAsync(Medication medication);
    Task DeleteAsync(int medicationId);
}