using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Commands;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;
using MediTrack.FollowUpService.API.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Controllers;

[ApiController]
[Route("api/v1/compliance")]
public class ComplianceController : ControllerBase
{
    private readonly IMedicationComplianceCommandService _complianceCommandService;
    private readonly IMedicationComplianceRepository _complianceRepository;
    private readonly RecordComplianceCommandFromResourceAssembler _commandAssembler;
    private readonly MedicationComplianceResourceFromEntityAssembler _responseAssembler;

    public ComplianceController(
        IMedicationComplianceCommandService complianceCommandService,
        IMedicationComplianceRepository complianceRepository,
        RecordComplianceCommandFromResourceAssembler commandAssembler,
        MedicationComplianceResourceFromEntityAssembler responseAssembler)
    {
        _complianceCommandService = complianceCommandService;
        _complianceRepository = complianceRepository;
        _commandAssembler = commandAssembler;
        _responseAssembler = responseAssembler;
    }

    [HttpPost]
    public async Task<ActionResult<MedicationComplianceResource>> RecordCompliance(
        [FromQuery] int patientId,
        [FromBody] RecordComplianceResource resource)
    {
        try
        {
            var command = _commandAssembler.ToCommand(patientId, resource);
            var compliance = await _complianceCommandService.HandleAsync(command);
            var responseResource = _responseAssembler.ToResource(compliance);

            return CreatedAtAction(
                actionName: nameof(GetComplianceById),
                routeValues: new { id = compliance.Id },
                value: responseResource);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MedicationComplianceResource>> GetComplianceById(int id)
    {
        var compliance = await _complianceRepository.FindByIdAsync(id);
        
        if (compliance == null)
        {
            return NotFound(new { message = $"Compliance record with id {id} not found" });
        }

        var responseResource = _responseAssembler.ToResource(compliance);
        return Ok(responseResource);
    }
}