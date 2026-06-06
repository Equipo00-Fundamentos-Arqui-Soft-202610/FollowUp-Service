using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Transform;

public class AdherenceHistoryResourceFromEntityAssembler
{
    public AdherenceHistoryResource ToResource(AdherenceHistory adherenceHistory)
    {
        var weeks = adherenceHistory.Weeks
            .Select(week => new WeeklyAdherenceResource(
                WeekStart: week.WeekStart.ToString("yyyy-MM-dd"),
                WeekEnd: week.WeekEnd.ToString("yyyy-MM-dd"),
                TakenDoses: week.TakenDoses,
                TotalDoses: week.TotalDoses,
                AdherencePercentage: week.AdherencePercentage))
            .ToList();

        return new AdherenceHistoryResource(
            OverallAdherencePercentage: adherenceHistory.OverallAdherencePercentage,
            Weeks: weeks);
    }
}
