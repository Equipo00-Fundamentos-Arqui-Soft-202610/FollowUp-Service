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
    private readonly INextPendingDoseQueryService _nextPendingDoseQueryService;
    private readonly MedicationResourceFromEntityAssembler _medicationAssembler;
    private readonly NextPendingDoseResourceFromEntityAssembler _nextDoseAssembler;

    public MedicationsController(
        IMedicationQueryService medicationQueryService,
        INextPendingDoseQueryService nextPendingDoseQueryService,
        MedicationResourceFromEntityAssembler medicationAssembler,
        NextPendingDoseResourceFromEntityAssembler nextDoseAssembler)
    {
        _medicationQueryService = medicationQueryService;
        _nextPendingDoseQueryService = nextPendingDoseQueryService;
        _medicationAssembler = medicationAssembler;
        _nextDoseAssembler = nextDoseAssembler;
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

            var resources = _medicationAssembler.ToResources(medications);
            return Ok(resources);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("next-dose")]
    public async Task<ActionResult<NextPendingDoseResource>> GetNextPendingDose(
        [FromQuery] int patientId)
    {
        try
        {
            var query = new GetNextPendingDoseQuery(patientId);
            var compliance = await _nextPendingDoseQueryService.HandleAsync(query);

            if (compliance == null)
                return NotFound(new { message = $"No pending doses found for patient {patientId} today" });

            var resource = _nextDoseAssembler.ToResource(compliance);
            return Ok(resource);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}