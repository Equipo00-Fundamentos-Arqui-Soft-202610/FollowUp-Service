namespace MediTrack.FollowUpService.API.Domain.Model.ValueObjects;

public class DoseValue
{
    public string Value { get; }

    public DoseValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("DoseValue cannot be empty or null", nameof(value));

        if (value.Length > 100)
            throw new ArgumentException("DoseValue cannot exceed 100 characters", nameof(value));

        Value = value.Trim();
    }

    public override bool Equals(object? obj)
    {
        return obj is DoseValue dv && dv.Value == Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    // Implicit conversion to string for EF Core compatibility
    public static implicit operator string(DoseValue dose) => dose.Value;

    // Implicit conversion from string for convenience
    public static implicit operator DoseValue(string value) => new(value);
}

