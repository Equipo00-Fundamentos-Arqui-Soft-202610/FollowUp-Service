using MediTrack.FollowUpService.API.Domain.Model.Aggregates;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface IAppointmentComplianceRepository
{
    Task AddAsync(AppointmentCompliance compliance);
    Task<AppointmentCompliance?> FindByIdAsync(int id);
    Task<IEnumerable<AppointmentCompliance>> FindByPatientIdAsync(int patientId);
}
