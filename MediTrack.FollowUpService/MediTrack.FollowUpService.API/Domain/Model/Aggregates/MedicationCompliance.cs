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
}