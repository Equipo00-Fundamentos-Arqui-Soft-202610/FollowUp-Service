namespace DefaultNamespace;

public class DoseSchedule
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public TimeOnly ScheduledTime { get; set; }
    public bool IsActive { get; set; }
    public Medication Medication { get; set; }
}