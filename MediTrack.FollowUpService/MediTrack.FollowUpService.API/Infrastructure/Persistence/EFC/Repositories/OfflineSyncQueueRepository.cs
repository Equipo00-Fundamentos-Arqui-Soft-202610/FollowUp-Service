using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Repositories;

public class OfflineSyncQueueRepository : IOfflineSyncQueueRepository
{
    private readonly FollowUpDbContext _context;

    public OfflineSyncQueueRepository(FollowUpDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OfflineSyncQueueItem item)
    {
        await _context.OfflineSyncQueueItems.AddAsync(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OfflineSyncQueueItem item)
    {
        _context.OfflineSyncQueueItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<OfflineSyncQueueItem>> FindByPatientIdAsync(int patientId)
    {
        return await _context.OfflineSyncQueueItems
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.QueuedAt)
            .ToListAsync();
    }
}
