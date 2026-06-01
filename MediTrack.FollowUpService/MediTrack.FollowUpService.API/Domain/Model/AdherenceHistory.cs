namespace MediTrack.FollowUpService.API.Domain.Model;

public record WeeklyAdherence(
    DateTime WeekStart,
    DateTime WeekEnd,
    int TakenDoses,
    int TotalDoses,
    decimal AdherencePercentage);

public record AdherenceHistory(
    decimal OverallAdherencePercentage,
    IReadOnlyCollection<WeeklyAdherence> Weeks);
