using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Queries;

namespace MediTrack.FollowUpService.API.Application.Internal.QueryServices;

public class AdherenceHistoryQueryService : IAdherenceHistoryQueryService
{
    private static readonly TimeZoneInfo LimaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    private readonly IMedicationComplianceRepository _complianceRepository;
    private readonly IMedicationRepository _medicationRepository;

    public AdherenceHistoryQueryService(
        IMedicationComplianceRepository complianceRepository,
        IMedicationRepository medicationRepository)
    {
        _complianceRepository = complianceRepository;
        _medicationRepository = medicationRepository;
    }

    public async Task<AdherenceHistory?> HandleAsync(GetAdherenceHistoryQuery query)
    {
        if (query.PatientId <= 0)
            throw new ArgumentException("PatientId must be greater than 0");

        var compliances = await _complianceRepository.FindByPatientIdAsync(query.PatientId);
        if (!compliances.Any())
            return null;

        var medications = await _medicationRepository.FindByPatientIdAsync(query.PatientId);

        var weeks = compliances
            .GroupBy(compliance => GetWeekStart(ToLimaDate(compliance.RecordedAt)))
            .Select(group =>
            {
                var weekStart = group.Key;
                var weekEnd = weekStart.AddDays(6);
                var totalDoses = CountActiveDoseOccurrencesForWeek(medications, weekStart);
                var takenDoses = group.Count(compliance => compliance.Status.IsTaken);

                return new WeeklyAdherence(
                    WeekStart: weekStart,
                    WeekEnd: weekEnd,
                    TakenDoses: takenDoses,
                    TotalDoses: totalDoses,
                    AdherencePercentage: CalculatePercentage(takenDoses, totalDoses));
            })
            .OrderByDescending(week => week.WeekStart)
            .ToList();

        var totalTakenDoses = weeks.Sum(week => week.TakenDoses);
        var totalDosesAcrossWeeks = weeks.Sum(week => week.TotalDoses);

        return new AdherenceHistory(
            OverallAdherencePercentage: CalculatePercentage(totalTakenDoses, totalDosesAcrossWeeks),
            Weeks: weeks);
    }

    private static DateTime ToLimaDate(DateTime dateTime)
    {
        return ToLimaDateTime(dateTime).Date;
    }

    private static DateTime ToLimaDateTime(DateTime dateTime)
    {
        var utcDateTime = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, LimaTimeZone);
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday).Date;
    }

    private static int CountActiveDoseOccurrencesForWeek(
        IEnumerable<Domain.Model.Aggregates.Medication> medications,
        DateTime weekStart)
    {
        var weekEndExclusive = weekStart.AddDays(7);
        var totalDoses = 0;

        foreach (var medication in medications)
        {
            var medicationStart = ToLimaDateTime(medication.StartDate);
            var medicationEnd = medication.EndDate.HasValue
                ? ToLimaDateTime(medication.EndDate.Value)
                : DateTime.MaxValue;

            foreach (var schedule in medication.Schedules.Where(schedule => schedule.IsActive))
            {
                for (var day = weekStart; day < weekEndExclusive; day = day.AddDays(1))
                {
                    var scheduledDoseAt = day.Date.Add(schedule.ScheduledTime.Value);
                    if (scheduledDoseAt >= medicationStart && scheduledDoseAt <= medicationEnd)
                        totalDoses++;
                }
            }
        }

        return totalDoses;
    }

    private static decimal CalculatePercentage(int takenDoses, int totalDoses)
    {
        if (totalDoses <= 0)
            return 0m;

        return Math.Round((decimal)takenDoses / totalDoses * 100m, 2);
    }
}
