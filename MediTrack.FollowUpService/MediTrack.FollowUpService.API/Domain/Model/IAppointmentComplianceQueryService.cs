using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Queries;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface IAppointmentComplianceQueryService
{
    Task<AppointmentCompliance?> Handle(GetAppointmentComplianceByIdQuery query);
    Task<IEnumerable<AppointmentCompliance>> Handle(GetAppointmentCompliancesByPatientQuery query);
}
