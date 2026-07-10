using System.Net.Http.Json;
using MediTrack.FollowUpService.API.Domain.Model;

namespace MediTrack.FollowUpService.API.Infrastructure.ExternalServices;

/// Llama directamente a Treatment-Service (servicio a servicio, sin pasar
/// por el API Gateway — igual que cualquier otro backend interno de esta
/// arquitectura). El endpoint `GET /api/v1/medications` no exige JWT en
/// Treatment-Service (confirmado: `MedicationsController` no tiene
/// `[Authorize]`), así que no hace falta propagar ningún token.
public class TreatmentMedicationsClient : ITreatmentMedicationsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TreatmentMedicationsClient> _logger;

    public TreatmentMedicationsClient(HttpClient httpClient, ILogger<TreatmentMedicationsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TreatmentMedicationDto>> GetMedicationsByPatientIdAsync(int patientId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/medications?patientId={patientId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Array.Empty<TreatmentMedicationDto>();

            response.EnsureSuccessStatusCode();

            var medications = await response.Content.ReadFromJsonAsync<List<TreatmentMedicationDto>>();
            return medications ?? new List<TreatmentMedicationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "No se pudo consultar Treatment-Service para respaldo de sincronización (patientId={PatientId})",
                patientId);
            return Array.Empty<TreatmentMedicationDto>();
        }
    }
}
