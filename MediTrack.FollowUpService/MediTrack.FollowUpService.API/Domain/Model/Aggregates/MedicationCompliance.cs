using MediTrack.FollowUpService.API.Domain.Model.ValueObjects;
using MediTrack.FollowUpService.API.Domain.Models;

namespace MediTrack.FollowUpService.API.Domain.Model.Aggregates;

public class MedicationCompliance
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoseScheduleId { get; set; }
    public ComplianceStatus Status { get; set; } = null!;
    public DateTime RecordedAt { get; set; }
    public string? VideoUrl { get; set; }
    public bool Synced { get; set; } = true;
    public DateTime? OfflineRecordedAt { get; set; }

    // Flujo de validación de evidencia en video (MediTrack AI Validator — Prototype).
    // `RecordedAt` ya cumple el rol de "SubmittedAt" (momento en que se creó el registro).
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public int? ValidatorId { get; set; }
    public string? RejectionReason { get; set; }
    public string? TemporaryVideoPath { get; set; }
    public DateTime? VideoExpiresAt { get; set; }

    public DoseSchedule DoseSchedule { get; set; } = null!;

    public MedicationCompliance() { }

    public MedicationCompliance(int patientId, int doseScheduleId, string status,
        string? videoUrl = null, DateTime? offlineRecordedAt = null)
    {
        PatientId = patientId;
        DoseScheduleId = doseScheduleId;
        Status = ComplianceStatus.From(status);
        VideoUrl = videoUrl;
        RecordedAt = DateTime.UtcNow;
        OfflineRecordedAt = offlineRecordedAt;
    }

    public bool IsTaken => Status.IsTaken;

    public void MarkAsSynced()
    {
        Synced = true;
    }

    /// Registra (o vuelve a registrar, en caso de reintento tras rechazo) evidencia en
    /// video pendiente de validación humana.
    public void SubmitVideoForValidation(string temporaryVideoPath, DateTime scheduledAt, DateTime videoExpiresAt)
    {
        Status = ComplianceStatus.PendingValidation;
        RecordedAt = DateTime.UtcNow;
        ScheduledAt = scheduledAt;
        TemporaryVideoPath = temporaryVideoPath;
        VideoExpiresAt = videoExpiresAt;
        ValidatedAt = null;
        ValidatorId = null;
        RejectionReason = null;
    }

    public void Approve(int? validatorId)
    {
        Status = ComplianceStatus.Approved;
        ValidatedAt = DateTime.UtcNow;
        ValidatorId = validatorId;
        TemporaryVideoPath = null;
    }

    public void Reject(int? validatorId, string? rejectionReason)
    {
        Status = ComplianceStatus.Rejected;
        ValidatedAt = DateTime.UtcNow;
        ValidatorId = validatorId;
        RejectionReason = rejectionReason;
        TemporaryVideoPath = null;
    }
}
