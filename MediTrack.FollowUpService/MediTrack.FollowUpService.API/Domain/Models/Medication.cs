namespace DefaultNamespace;

public class Medication
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string Name { get; set; }
    public string Dose { get; set; }
    public int FrequencyHours { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int StockCount { get; set; }
    public List<DoseSchedule> Schedules { get; set; } = new();
}