using System.Security.Claims;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.ValueObjects;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Controllers;

/// Flujo de validación de evidencia en video — MediTrack AI Validator (Prototype).
///
/// IMPORTANTE: la decisión de aprobar/rechazar en esta versión la toma un
/// validador HUMANO desde una aplicación web separada (human-in-the-loop). No
/// hay ningún modelo de inteligencia artificial real analizando el video en
/// este código. El contrato de entrada/salida (subir evidencia -> quedar
/// "PendingValidation" -> recibir una decisión "Approved"/"Rejected") queda
/// preparado para que, a futuro, un modelo real de visión artificial reemplace
/// al validador humano sin cambiar esta forma de datos.
[ApiController]
[Authorize]
[Route("api/v1/compliance")]
public class ComplianceVideoController : ControllerBase
{
    private static readonly string[] AllowedContentTypes = { "video/mp4", "video/quicktime", "video/x-msvideo" };
    private const long MaxVideoSizeBytes = 50 * 1024 * 1024; // 50 MB — suficiente para un clip de ~30s.
    private static readonly TimeZoneInfo LimaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    private readonly FollowUpDbContext _context;
    private readonly IMedicationComplianceRepository _complianceRepository;
    private readonly ITemporaryVideoStorage _videoStorage;
    private readonly ILogger<ComplianceVideoController> _logger;

    public ComplianceVideoController(
        FollowUpDbContext context,
        IMedicationComplianceRepository complianceRepository,
        ITemporaryVideoStorage videoStorage,
        ILogger<ComplianceVideoController> logger)
    {
        _context = context;
        _complianceRepository = complianceRepository;
        _videoStorage = videoStorage;
        _logger = logger;
    }

    /// El paciente graba y sube su evidencia. Máximo ~30s se valida en el
    /// cliente (Mobile); aquí se valida tipo y un tamaño razonable — no hay
    /// forma de inspeccionar la duración real de un video sin una librería de
    /// procesamiento multimedia, que este prototipo no incorpora (limitación
    /// documentada).
    [HttpPost("video")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(VideoSubmissionResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VideoSubmissionResource>> SubmitVideo(
        [FromForm] int doseScheduleId,
        IFormFile video)
    {
        var patientId = ResolvePatientId();
        if (patientId == null)
            return Unauthorized(new { message = "No se pudo determinar el paciente autenticado." });

        if (video == null || video.Length == 0)
            return BadRequest(new { message = "No se recibió ningún archivo de video." });

        if (!AllowedContentTypes.Contains(video.ContentType?.ToLowerInvariant()))
            return BadRequest(new { message = "Solo se aceptan videos MP4, MOV o AVI." });

        if (video.Length > MaxVideoSizeBytes)
            return BadRequest(new { message = "El video excede el tamaño máximo de 50 MB." });

        var doseSchedule = await _context.DoseSchedules
            .Include(ds => ds.Medication)
            .FirstOrDefaultAsync(ds => ds.Id == doseScheduleId);

        if (doseSchedule == null)
            return BadRequest(new { message = $"No existe el horario de dosis con id {doseScheduleId}." });

        var today = NowInLima().Date;
        var scheduledAtUtc = ComputeTodayScheduledAtUtc(doseSchedule.ScheduledTime.Value);
        var videoExpiresAt = DateTime.UtcNow.AddHours(24);

        var extension = video.ContentType?.ToLowerInvariant() switch
        {
            "video/mp4" => ".mp4",
            "video/quicktime" => ".mov",
            "video/x-msvideo" => ".avi",
            _ => ".mp4"
        };

        string fileName;
        await using (var stream = video.OpenReadStream())
        {
            fileName = await _videoStorage.SaveAsync(stream, extension);
        }

        // Upsert: si ya había un intento pendiente/rechazado de hoy para esta
        // misma dosis (reintento tras rechazo), se reemplaza en vez de duplicar filas.
        var existing = await _complianceRepository.FindTodayValidationAttemptAsync(patientId.Value, doseScheduleId, today);

        MedicationCompliance compliance;
        if (existing != null)
        {
            // Si el intento anterior seguía teniendo un video temporal (no debería,
            // ya que approve/reject lo borran, pero un rechazo sí lo hace), se limpia.
            if (!string.IsNullOrEmpty(existing.TemporaryVideoPath))
                _videoStorage.Delete(existing.TemporaryVideoPath);

            existing.SubmitVideoForValidation(fileName, scheduledAtUtc, videoExpiresAt);
            await _complianceRepository.UpdateAsync(existing);
            compliance = existing;
        }
        else
        {
            compliance = new MedicationCompliance(patientId.Value, doseScheduleId, ComplianceStatus.PendingValidation.Value);
            compliance.SubmitVideoForValidation(fileName, scheduledAtUtc, videoExpiresAt);
            await _complianceRepository.AddAsync(compliance);
        }

        return StatusCode(StatusCodes.Status201Created,
            new VideoSubmissionResource(compliance.Id, compliance.Status.Value));
    }

    /// Bandeja del validador: todos los casos pendientes, de cualquier paciente.
    [HttpGet("pending-validation")]
    [ProducesResponseType(typeof(IEnumerable<PendingValidationItemResource>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PendingValidationItemResource>>> GetPendingValidation()
    {
        var pending = await _complianceRepository.FindPendingValidationAsync();

        var items = pending.Select(mc => new PendingValidationItemResource(
            ComplianceId: mc.Id,
            PatientId: mc.PatientId,
            MedicationName: mc.DoseSchedule.Medication.Name,
            Dose: mc.DoseSchedule.Medication.Dose.Value,
            ScheduledAt: mc.ScheduledAt,
            SubmittedAt: mc.RecordedAt,
            VideoEndpoint: $"/api/v1/compliance/{mc.Id}/video"
        ));

        return Ok(items);
    }

    /// Streaming controlado del video temporal — solo mientras esté pendiente.
    [HttpGet("{id}/video")]
    public async Task<IActionResult> GetVideo(int id)
    {
        var compliance = await _complianceRepository.FindByIdAsync(id);
        if (compliance == null)
            return NotFound(new { message = $"No existe el cumplimiento con id {id}." });

        if (!compliance.Status.IsPendingValidation || string.IsNullOrEmpty(compliance.TemporaryVideoPath))
            return NotFound(new { message = "El video ya no está disponible (el caso ya fue validado o expiró)." });

        var stream = _videoStorage.OpenRead(compliance.TemporaryVideoPath);
        if (stream == null)
            return NotFound(new { message = "El archivo de video no se encontró en el almacenamiento temporal." });

        var contentType = Path.GetExtension(compliance.TemporaryVideoPath).ToLowerInvariant() switch
        {
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            _ => "video/mp4"
        };

        return File(stream, contentType, enableRangeProcessing: true);
    }

    /// Consulta de estado — usada por Mobile para el polling.
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(ComplianceValidationStatusResource), StatusCodes.Status200OK)]
    public async Task<ActionResult<ComplianceValidationStatusResource>> GetStatus(int id)
    {
        var compliance = await _complianceRepository.FindByIdAsync(id);
        if (compliance == null)
            return NotFound(new { message = $"No existe el cumplimiento con id {id}." });

        return Ok(ToStatusResource(compliance));
    }

    /// Aprobar — hecho hoy por un validador humano (ver nota de clase). Cuenta
    /// para adherencia porque ComplianceStatus.IsTaken incluye "approved".
    [HttpPatch("{id}/approve")]
    [ProducesResponseType(typeof(ComplianceValidationStatusResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ComplianceValidationStatusResource>> Approve(int id)
    {
        var compliance = await _complianceRepository.FindByIdAsync(id);
        if (compliance == null)
            return NotFound(new { message = $"No existe el cumplimiento con id {id}." });

        if (!compliance.Status.IsPendingValidation)
            return BadRequest(new { message = "Solo se puede aprobar un cumplimiento en estado PendingValidation." });

        if (!string.IsNullOrEmpty(compliance.TemporaryVideoPath))
            _videoStorage.Delete(compliance.TemporaryVideoPath);

        compliance.Approve(ResolveValidatorId());
        await _complianceRepository.UpdateAsync(compliance);

        return Ok(ToStatusResource(compliance));
    }

    /// Rechazar — el paciente podrá reintentar (mismo endpoint de subida) si
    /// todavía está dentro de la ventana de toma (regla que valida Mobile).
    [HttpPatch("{id}/reject")]
    [ProducesResponseType(typeof(ComplianceValidationStatusResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ComplianceValidationStatusResource>> Reject(int id, [FromBody] RejectComplianceResource? resource)
    {
        var compliance = await _complianceRepository.FindByIdAsync(id);
        if (compliance == null)
            return NotFound(new { message = $"No existe el cumplimiento con id {id}." });

        if (!compliance.Status.IsPendingValidation)
            return BadRequest(new { message = "Solo se puede rechazar un cumplimiento en estado PendingValidation." });

        if (!string.IsNullOrEmpty(compliance.TemporaryVideoPath))
            _videoStorage.Delete(compliance.TemporaryVideoPath);

        compliance.Reject(ResolveValidatorId(), resource?.RejectionReason);
        await _complianceRepository.UpdateAsync(compliance);

        return Ok(ToStatusResource(compliance));
    }

    private static ComplianceValidationStatusResource ToStatusResource(MedicationCompliance compliance) => new(
        ComplianceId: compliance.Id,
        Status: compliance.Status.Value,
        RejectionReason: compliance.RejectionReason,
        ValidatedAt: compliance.ValidatedAt
    );

    private static DateTime NowInLima() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LimaTimeZone);

    private static DateTime ComputeTodayScheduledAtUtc(TimeSpan scheduledTime)
    {
        var occurrenceLima = DateTime.SpecifyKind(NowInLima().Date.Add(scheduledTime), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(occurrenceLima, LimaTimeZone);
    }

    /// Se obtiene del usuario autenticado, nunca del cliente (evita IDOR): claim
    /// "patientId" del JWT si existe; si no, se usa el "sub" (id del usuario en
    /// Identity-Service) — mismo criterio de resguardo que ya usa Mobile.
    private int? ResolvePatientId()
    {
        var claim = User.FindFirst("patientId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private int? ResolveValidatorId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
