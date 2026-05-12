using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Queries;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;
using MediTrack.FollowUpService.API.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Controllers;

[ApiController]
[Route("api/v1/medications")]
public class MedicationsController : ControllerBase
{
    private readonly IMedicationQueryService _medicationQueryService;
    private readonly MedicationResourceFromEntityAssembler _assembler;

    public MedicationsController(
        IMedicationQueryService medicationQueryService,
        MedicationResourceFromEntityAssembler assembler)
    {
        _medicationQueryService = medicationQueryService;
        _assembler = assembler;
    }

    [HttpGet]
    public async Task<ActionResult<ICollection<MedicationResource>>> GetMedicationsByPatientId(
        [FromQuery] int patientId)
    {
        try
        {
            var query = new GetMedicationsByPatientIdQuery(patientId);
            var medications = await _medicationQueryService.HandleAsync(query);

            if (!medications.Any())
                return NotFound(new { message = $"No medications found for patient {patientId}" });

            var resources = _assembler.ToResources(medications);
            return Ok(resources);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}