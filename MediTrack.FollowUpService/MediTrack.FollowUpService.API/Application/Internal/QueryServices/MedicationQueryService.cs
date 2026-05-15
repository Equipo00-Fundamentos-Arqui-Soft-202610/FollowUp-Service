using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Queries;

namespace MediTrack.FollowUpService.API.Application.Internal.QueryServices;

public class MedicationQueryService : IMedicationQueryService
{
    private readonly IMedicationRepository _medicationRepository;

    public MedicationQueryService(IMedicationRepository medicationRepository)
    {
        _medicationRepository = medicationRepository;
    }

    public async Task<ICollection<Medication>> HandleAsync(GetMedicationsByPatientIdQuery query)
    {
        if (query.PatientId <= 0)
            throw new ArgumentException("PatientId must be greater than 0");

        var medications = await _medicationRepository.FindByPatientIdAsync(query.PatientId);
        return medications;
    }
}