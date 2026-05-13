namespace MediTrack.FollowUpService.API.Domain.Model.ValueObjects;

public class ScheduledHour
{
    public TimeSpan Value { get; }

    public ScheduledHour(TimeSpan value)
    {
        if (value.TotalHours < 0 || value.TotalHours >= 24)
            throw new ArgumentException("ScheduledHour must be between 00:00 and 23:59", nameof(value));

        Value = value;
    }

    public static ScheduledHour From(int hours, int minutes = 0, int seconds = 0)
    {
        if (hours < 0 || hours >= 24)
            throw new ArgumentException("Hours must be between 0 and 23", nameof(hours));

        if (minutes < 0 || minutes >= 60)
            throw new ArgumentException("Minutes must be between 0 and 59", nameof(minutes));

        if (seconds < 0 || seconds >= 60)
            throw new ArgumentException("Seconds must be between 0 and 59", nameof(seconds));

        return new ScheduledHour(new TimeSpan(0, hours, minutes, seconds));
    }

    public int Hours => Value.Hours;
    public int Minutes => Value.Minutes;
    public int Seconds => Value.Seconds;

    public override bool Equals(object? obj)
    {
        return obj is ScheduledHour sh && sh.Value == Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value.ToString("hh\\:mm\\:ss");
    }

    // Implicit conversion to TimeSpan for EF Core compatibility
    public static implicit operator TimeSpan(ScheduledHour hour) => hour.Value;

    // Implicit conversion from TimeSpan for convenience
    public static implicit operator ScheduledHour(TimeSpan value) => new(value);
}

