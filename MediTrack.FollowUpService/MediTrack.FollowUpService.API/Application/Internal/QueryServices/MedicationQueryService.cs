using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Queries;

namespace MediTrack.FollowUpService.API.Application.Internal.QueryServices;

public class MedicationQueryService : IMedicationQueryService
{
    private readonly IMedicationRepository _medicationRepository;
    private readonly IMedicationReplicaSyncService _replicaSyncService;

    public MedicationQueryService(
        IMedicationRepository medicationRepository,
        IMedicationReplicaSyncService replicaSyncService)
    {
        _medicationRepository = medicationRepository;
        _replicaSyncService = replicaSyncService;
    }

    public async Task<ICollection<Medication>> HandleAsync(GetMedicationsByPatientIdQuery query)
    {
        if (query.PatientId <= 0)
            throw new ArgumentException("PatientId must be greater than 0");

        // Mismo respaldo que next-dose: ver docs/next-dose-sync-fix.md.
        await _replicaSyncService.EnsureSyncedAsync(query.PatientId);

        var medications = await _medicationRepository.FindByPatientIdAsync(query.PatientId);
        return medications;
    }
}
