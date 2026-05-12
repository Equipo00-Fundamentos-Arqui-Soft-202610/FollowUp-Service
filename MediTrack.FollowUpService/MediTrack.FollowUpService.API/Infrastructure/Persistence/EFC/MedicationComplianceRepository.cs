using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;

public class MedicationComplianceRepository : IMedicationComplianceRepository
{
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
}