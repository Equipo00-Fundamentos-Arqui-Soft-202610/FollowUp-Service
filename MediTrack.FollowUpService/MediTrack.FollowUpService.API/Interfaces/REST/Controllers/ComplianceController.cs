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
    private readonly RecordComplianceCommandFromResourceAssembler _commandAssembler;
    private readonly MedicationComplianceResourceFromEntityAssembler _responseAssembler;

    public ComplianceController(
        IMedicationComplianceCommandService complianceCommandService,
        RecordComplianceCommandFromResourceAssembler commandAssembler,
        MedicationComplianceResourceFromEntityAssembler responseAssembler)
    {
        _complianceCommandService = complianceCommandService;
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
        // This endpoint is referenced by CreatedAtAction above
        // You can implement it for completeness
        return Ok();
    }
}