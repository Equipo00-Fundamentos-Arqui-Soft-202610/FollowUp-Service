using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Repositories;

public class AppointmentComplianceRepository : IAppointmentComplianceRepository
{
    private readonly FollowUpDbContext _context;

    public AppointmentComplianceRepository(FollowUpDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AppointmentCompliance compliance)
    {
        await _context.AppointmentCompliances.AddAsync(compliance);
        await _context.SaveChangesAsync();
    }

    public async Task<AppointmentCompliance?> FindByIdAsync(int id)
    {
        return await _context.AppointmentCompliances.FindAsync(id);
    }

    public async Task<IEnumerable<AppointmentCompliance>> FindByPatientIdAsync(int patientId)
    {
        return await _context.AppointmentCompliances
            .Where(ac => ac.PatientId == patientId)
            .OrderByDescending(ac => ac.RecordedAt)
            .ToListAsync();
    }
}
