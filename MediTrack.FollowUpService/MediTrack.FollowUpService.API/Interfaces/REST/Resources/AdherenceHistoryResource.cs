namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record WeeklyAdherenceResource(
    string WeekStart,
    string WeekEnd,
    int TakenDoses,
    int TotalDoses,
    decimal AdherencePercentage);

public record AdherenceHistoryResource(
    decimal OverallAdherencePercentage,
    IReadOnlyCollection<WeeklyAdherenceResource> Weeks);
