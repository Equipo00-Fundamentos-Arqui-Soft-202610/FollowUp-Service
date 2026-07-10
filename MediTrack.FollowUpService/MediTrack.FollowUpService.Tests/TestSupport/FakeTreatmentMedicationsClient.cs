using MediTrack.FollowUpService.API.Domain.Model;

namespace MediTrack.FollowUpService.Tests.TestSupport;

public class FakeTreatmentMedicationsClient : ITreatmentMedicationsClient
{
    private readonly IReadOnlyList<TreatmentMedicationDto> _medications;
    public int CallCount { get; private set; }

    public FakeTreatmentMedicationsClient(IReadOnlyList<TreatmentMedicationDto> medications)
    {
        _medications = medications;
    }

    public Task<IReadOnlyList<TreatmentMedicationDto>> GetMedicationsByPatientIdAsync(int patientId)
    {
        CallCount++;
        return Task.FromResult(_medications);
    }
}
