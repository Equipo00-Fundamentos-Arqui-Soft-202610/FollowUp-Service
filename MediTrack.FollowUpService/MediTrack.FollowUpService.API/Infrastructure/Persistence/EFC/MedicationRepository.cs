
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;

public class MedicationRepository : IMedicationRepository
{
    private readonly FollowUpDbContext _context;

    public MedicationRepository(FollowUpDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Medication>> FindByPatientIdAsync(int patientId)
    {
        return await _context.Medications
            .Where(m => m.PatientId == patientId)
            .Include(m => m.Schedules)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<Medication?> FindByIdAsync(int medicationId)
    {
        return await _context.Medications
            .Include(m => m.Schedules)
            .FirstOrDefaultAsync(m => m.Id == medicationId);
    }

    public async Task AddAsync(Medication medication)
    {
        await _context.Medications.AddAsync(medication);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Medication medication)
    {
        _context.Medications.Update(medication);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int medicationId)
    {
        var medication = await FindByIdAsync(medicationId);
        if (medication != null)
        {
            _context.Medications.Remove(medication);
            await _context.SaveChangesAsync();
        }
    }
}