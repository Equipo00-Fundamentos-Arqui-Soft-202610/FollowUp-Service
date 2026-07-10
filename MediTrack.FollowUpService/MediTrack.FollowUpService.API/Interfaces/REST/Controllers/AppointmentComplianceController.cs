using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Commands;
using MediTrack.FollowUpService.API.Domain.Model.Queries;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;
using MediTrack.FollowUpService.API.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/appointment-compliance")]
public class AppointmentComplianceController : ControllerBase
{
    private readonly IAppointmentComplianceCommandService _commandService;
    private readonly IAppointmentComplianceQueryService _queryService;

    public AppointmentComplianceController(
        IAppointmentComplianceCommandService commandService,
        IAppointmentComplianceQueryService queryService)
    {
        _commandService = commandService;
        _queryService = queryService;
    }

    [HttpPost]
    public async Task<IActionResult> RecordCompliance(
        [FromQuery] int patientId,
        [FromBody] RecordAppointmentComplianceResource resource)
    {
        try
        {
            if (patientId <= 0)
                return BadRequest(new { message = "patientId must be positive" });
            if (patientId != resource.PatientId)
                return BadRequest(new { message = "patientId in query and body must match" });

            var command = RecordAppointmentComplianceCommandFromResourceAssembler
                .ToCommandFromResource(resource);
            var entity = await _commandService.Handle(command);
            if (entity is null) return BadRequest();

            var result = AppointmentComplianceResourceFromEntityAssembler
                .ToResourceFromEntity(entity);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var entity = await _queryService.Handle(new GetAppointmentComplianceByIdQuery(id));
        if (entity is null) return NotFound();
        return Ok(AppointmentComplianceResourceFromEntityAssembler.ToResourceFromEntity(entity));
    }

    [HttpGet]
    public async Task<IActionResult> GetByPatient([FromQuery] int patientId)
    {
        if (patientId <= 0)
            return BadRequest(new { message = "patientId must be positive" });

        var entities = await _queryService.Handle(new GetAppointmentCompliancesByPatientQuery(patientId));
        var resources = entities.Select(AppointmentComplianceResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(resources);
    }
}
