using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Queries;

namespace MediTrack.FollowUpService.API.Application.Internal.QueryServices;

public class NextPendingDoseQueryService : INextPendingDoseQueryService
{
    private readonly IMedicationRepository _medicationRepository;
    private readonly IMedicationComplianceRepository _complianceRepository;

    public NextPendingDoseQueryService(
        IMedicationRepository medicationRepository,
        IMedicationComplianceRepository complianceRepository)
    {
        _medicationRepository = medicationRepository;
        _complianceRepository = complianceRepository;
    }

    public async Task<MedicationCompliance?> HandleAsync(GetNextPendingDoseQuery query)
    {
        if (query.PatientId <= 0)
            throw new ArgumentException("PatientId must be greater than 0");

        // Get all active medications for the patient 
        var medications = await _medicationRepository.FindByPatientIdAsync(query.PatientId);
        var activeMedications = medications.Where(m => m.IsActive).ToList();

        if (!activeMedications.Any())
            return null;

        // Get all active dose schedules from active medications
        var activeDoseSchedules = activeMedications
            .SelectMany(m => m.Schedules)
            .Where(s => s.IsActive)
            .ToList();

        if (!activeDoseSchedules.Any())
            return null;

       

        // Get Lima Hour
        var limaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, limaTimeZone);
        var today = now.Date;
        
        
        // Get all compliance records for today
        var complianceRecords = await _complianceRepository.FindByPatientIdAsync(query.PatientId);
        var todayCompliances = complianceRecords
            .Where(c => c.RecordedAt.Date == today)
            .ToList();
        
        var nextPendingSchedule = activeDoseSchedules
            .Where(s =>
            {
                var hasTakenCompliance = todayCompliances.Any(c =>
                    c.DoseScheduleId == s.Id && c.Status.IsTaken);
                return !hasTakenCompliance;
            })
            .OrderBy(s => s.ScheduledTime.Value > now.TimeOfDay ? 0 : 1) 
            .ThenBy(s => s.ScheduledTime.Value)
            .FirstOrDefault();

        if (nextPendingSchedule == null)
            return null;

        // Create a virtual MedicationCompliance object to return dose information
        // The DoseSchedule already has the Medication relationship loaded
        var doseCompliance = new MedicationCompliance
        {
            DoseScheduleId = nextPendingSchedule.Id,
            DoseSchedule = nextPendingSchedule,
            PatientId = query.PatientId
        };

        return doseCompliance;
    }
}

