using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Queries;

namespace MediTrack.FollowUpService.API.Application.Internal.QueryServices;

public class AppointmentComplianceQueryService : IAppointmentComplianceQueryService
{
    private readonly IAppointmentComplianceRepository _repository;

    public AppointmentComplianceQueryService(IAppointmentComplianceRepository repository)
    {
        _repository = repository;
    }

    public async Task<AppointmentCompliance?> Handle(GetAppointmentComplianceByIdQuery query)
    {
        return await _repository.FindByIdAsync(query.Id);
    }

    public async Task<IEnumerable<AppointmentCompliance>> Handle(GetAppointmentCompliancesByPatientQuery query)
    {
        return await _repository.FindByPatientIdAsync(query.PatientId);
    }
}
