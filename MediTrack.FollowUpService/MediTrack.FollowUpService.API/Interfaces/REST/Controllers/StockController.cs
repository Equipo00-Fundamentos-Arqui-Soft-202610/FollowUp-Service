using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Queries;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;
using MediTrack.FollowUpService.API.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Controllers;

[ApiController]
[Route("api/v1/stock")]
public class StockController : ControllerBase
{
    private readonly ILowStockMedicationQueryService _lowStockMedicationQueryService;
    private readonly LowStockMedicationResourceFromEntityAssembler _lowStockMedicationAssembler;

    public StockController(
        ILowStockMedicationQueryService lowStockMedicationQueryService,
        LowStockMedicationResourceFromEntityAssembler lowStockMedicationAssembler)
    {
        _lowStockMedicationQueryService = lowStockMedicationQueryService;
        _lowStockMedicationAssembler = lowStockMedicationAssembler;
    }

    [HttpGet("low")]
    public async Task<ActionResult<ICollection<LowStockMedicationResource>>> GetLowStockMedications(
        [FromQuery] int patientId)
    {
        try
        {
            var query = new GetLowStockMedicationsQuery(patientId);
            var medications = await _lowStockMedicationQueryService.HandleAsync(query);

            if (!medications.Any())
                return NotFound(new { message = $"No low stock medications found for patient {patientId}" });

            var resources = _lowStockMedicationAssembler.ToResources(medications);
            return Ok(resources);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
