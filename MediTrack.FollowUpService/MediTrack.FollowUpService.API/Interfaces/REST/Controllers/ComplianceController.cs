using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Commands;
using MediTrack.FollowUpService.API.Interfaces.REST.Filters;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;
using MediTrack.FollowUpService.API.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Controllers;

[ApiController]
[Authorize]
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
    [TypeFilter(typeof(IdempotencyFilterAttribute))]
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
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedicationComplianceResource>>> GetRecentCompliance(
        [FromQuery] int patientId,
        [FromQuery] int limit = 10)
    {
        if (patientId <= 0)
            return BadRequest(new { message = "patientId must be greater than 0" });

        if (limit <= 0)
            limit = 10;

        var compliances = await _complianceRepository.FindByPatientIdAsync(patientId);

        var recentCompliances = compliances
            .Take(limit)
            .ToList();

        if (!recentCompliances.Any())
            return NotFound(new { message = $"No compliance records found for patient {patientId}" });

        var resources = _responseAssembler.ToResources(recentCompliances);
        return Ok(resources);
    }

    [HttpGet("stats/by-medication")]
    public async Task<ActionResult<IEnumerable<MedicationComplianceByMedicationResource>>> GetComplianceStatsByMedication(
        [FromQuery] int? patientId)
    {
        var compliances = patientId.HasValue
            ? await _complianceRepository.FindByPatientIdAsync(patientId.Value)
            : await _complianceRepository.FindAllAsync();

        var stats = compliances
            .Where(mc => mc.DoseSchedule?.Medication != null)
            .GroupBy(mc => mc.DoseSchedule.Medication.Name)
            .Select(g => new MedicationComplianceByMedicationResource(
                g.Key,
                g.Count(),
                g.Count(mc => mc.Status.IsTaken),
                g.Count(mc => !mc.Status.IsTaken),
                g.Count() > 0 ? Math.Round((double)g.Count(mc => mc.Status.IsTaken) / g.Count() * 100, 2) : 0))
            .ToList();

        return Ok(stats);
    }
}

public record MedicationComplianceByMedicationResource(
    string MedicationName,
    int TotalDoses,
    int TakenDoses,
    int MissedDoses,
    double ComplianceRate);