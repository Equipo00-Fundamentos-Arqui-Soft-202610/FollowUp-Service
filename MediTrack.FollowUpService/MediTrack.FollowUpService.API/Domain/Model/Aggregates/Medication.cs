using MediTrack.FollowUpService.API.Domain.Model.ValueObjects;
using MediTrack.FollowUpService.API.Domain.Models;

namespace MediTrack.FollowUpService.API.Domain.Model.Aggregates;


public class Medication
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string Name { get; set; } = null!;
    public DoseValue Dose { get; set; } = null!;
    public int FrequencyHours { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int StockCount { get; set; }
    public int StockAlertThreshold { get; set; }
    public List<DoseSchedule> Schedules { get; set; } = new();

    public Medication() { }

    public Medication(int patientId, string name, string dose, int frequencyHours, 
        DateTime startDate, int stockCount)
    {
        PatientId = patientId;
        Name = name;
        Dose = new DoseValue(dose);
        FrequencyHours = frequencyHours;
        StartDate = startDate;
        StockCount = stockCount;
    }

    public void UpdateStockCount(int newCount)
    {
        if (newCount < 0)
            throw new ArgumentException("Stock count cannot be negative");
        StockCount = newCount;
    }

    public bool IsActive => EndDate == null || EndDate > DateTime.UtcNow;

    public static Medication CreateFromEvent(int id, int patientId, string name, string dose,
        int frequencyHours, DateTime startDate, DateTime? endDate, int stockCount, int stockAlertThreshold)
    {
        return new Medication
        {
            Id = id,
            PatientId = patientId,
            Name = name,
            Dose = new DoseValue(dose),
            FrequencyHours = frequencyHours,
            StartDate = startDate,
            EndDate = endDate,
            StockCount = stockCount,
            StockAlertThreshold = stockAlertThreshold
        };
    }
}