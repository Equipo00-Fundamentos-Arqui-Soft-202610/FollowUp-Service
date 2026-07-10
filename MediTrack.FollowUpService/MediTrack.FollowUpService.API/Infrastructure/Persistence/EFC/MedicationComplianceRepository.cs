using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;

public class MedicationComplianceRepository : IMedicationComplianceRepository
{
    private static readonly TimeZoneInfo LimaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    private readonly FollowUpDbContext _context;

    public MedicationComplianceRepository(FollowUpDbContext context)
    {
        _context = context;
    }

    public async Task<MedicationCompliance?> FindByIdAsync(int complianceId)
    {
        return await _context.MedicationCompliances
            .Include(mc => mc.DoseSchedule)
            .FirstOrDefaultAsync(mc => mc.Id == complianceId);
    }

    public async Task<ICollection<MedicationCompliance>> FindByPatientIdAsync(int patientId)
    {
        return await _context.MedicationCompliances
            .Where(mc => mc.PatientId == patientId)
            .Include(mc => mc.DoseSchedule)
            .ThenInclude(ds => ds.Medication)
            .OrderByDescending(mc => mc.RecordedAt)
            .ToListAsync();
    }

    public async Task<ICollection<MedicationCompliance>> FindByDoseScheduleIdAsync(int doseScheduleId)
    {
        return await _context.MedicationCompliances
            .Where(mc => mc.DoseScheduleId == doseScheduleId)
            .OrderByDescending(mc => mc.RecordedAt)
            .ToListAsync();
    }

    public async Task AddAsync(MedicationCompliance compliance)
    {
        await _context.MedicationCompliances.AddAsync(compliance);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(MedicationCompliance compliance)
    {
        _context.MedicationCompliances.Update(compliance);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int complianceId)
    {
        var compliance = await FindByIdAsync(complianceId);
        if (compliance != null)
        {
            _context.MedicationCompliances.Remove(compliance);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ICollection<MedicationCompliance>> FindPendingValidationAsync()
    {
        // Se filtra en memoria (no en SQL) con el VO ComplianceStatus, igual que ya
        // hace NextPendingDoseQueryService — evita depender de que EF traduzca
        // comparaciones sobre el value converter de Status.
        var all = await _context.MedicationCompliances
            .Include(mc => mc.DoseSchedule)
            .ThenInclude(ds => ds.Medication)
            .OrderBy(mc => mc.RecordedAt)
            .ToListAsync();

        return all.Where(mc => mc.Status.IsPendingValidation).ToList();
    }

    public async Task<MedicationCompliance?> FindTodayValidationAttemptAsync(int patientId, int doseScheduleId, DateTime today)
    {
        var candidates = await _context.MedicationCompliances
            .Where(mc => mc.PatientId == patientId && mc.DoseScheduleId == doseScheduleId)
            .OrderByDescending(mc => mc.RecordedAt)
            .ToListAsync();

        // `RecordedAt` se guarda en UTC; "today" se calcula en hora Lima (mismo
        // criterio que el resto del servicio, ver AdherenceHistoryQueryService),
        // así que hay que convertir antes de comparar por fecha calendario.
        return candidates.FirstOrDefault(mc =>
            ToLimaDate(mc.RecordedAt) == today.Date && (mc.Status.IsPendingValidation || mc.Status.IsRejected));
    }

    private static DateTime ToLimaDate(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, LimaTimeZone).Date;
    }

    public async Task<ICollection<MedicationCompliance>> FindAllAsync()
    {
        return await _context.MedicationCompliances
            .Include(mc => mc.DoseSchedule)
            .ThenInclude(ds => ds.Medication)
            .OrderByDescending(mc => mc.RecordedAt)
            .ToListAsync();
    }
}