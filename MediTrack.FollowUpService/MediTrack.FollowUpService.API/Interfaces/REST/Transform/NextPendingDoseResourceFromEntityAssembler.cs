using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Transform;

public class NextPendingDoseResourceFromEntityAssembler
{
    public NextPendingDoseResource ToResource(MedicationCompliance compliance)
    {
        var medication = compliance.DoseSchedule.Medication;
        var scheduledTime = compliance.DoseSchedule.ScheduledTime.Value;

        var limaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        var nowLima = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, limaTimeZone);
        var nowTime = nowLima.TimeOfDay;

        int minutesUntilDose;
        if (scheduledTime > nowTime)
        {
            minutesUntilDose = (int)(scheduledTime - nowTime).TotalMinutes;
        }
        else
        {
            var timeUntilMidnight = TimeSpan.FromHours(24) - nowTime;
            minutesUntilDose = (int)(timeUntilMidnight.Add(scheduledTime)).TotalMinutes;
        }

        // Instante de la ocurrencia de HOY (haya pasado ya o esté por venir), en
        // hora Lima, convertido a UTC — es lo que el cliente necesita para
        // calcular la ventana de toma sin ambigüedad de zona horaria.
        var scheduledOccurrenceLima = DateTime.SpecifyKind(nowLima.Date.Add(scheduledTime), DateTimeKind.Unspecified);
        var scheduledAtUtc = TimeZoneInfo.ConvertTimeToUtc(scheduledOccurrenceLima, limaTimeZone);

        var validationStatus = compliance.Status?.IsPendingValidation == true ? "PendingValidation"
            : compliance.Status?.IsRejected == true ? "Rejected"
            : null;

        return new NextPendingDoseResource(
            DoseScheduleId: compliance.DoseScheduleId,
            MedicationName: medication.Name,
            Dose: medication.Dose.Value,
            ScheduledTime: scheduledTime.ToString(@"hh\:mm"),
            MinutesUntilDose: minutesUntilDose,
            ScheduledAtUtc: scheduledAtUtc.ToString("o"),
            ComplianceId: compliance.Id == 0 ? null : compliance.Id,
            ValidationStatus: validationStatus,
            RejectionReason: compliance.RejectionReason
        );
    }
}
