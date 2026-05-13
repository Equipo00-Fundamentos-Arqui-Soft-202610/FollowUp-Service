namespace MediTrack.FollowUpService.API.Domain.Model.ValueObjects;

public class ComplianceStatus
{
    public static readonly ComplianceStatus Taken = new("taken");
    public static readonly ComplianceStatus Skipped = new("skipped");

    private static readonly Dictionary<string, ComplianceStatus> _validStatuses = new()
    {
        { "taken", Taken },
        { "skipped", Skipped }
    };

    public string Value { get; }

    private ComplianceStatus(string value)
    {
        Value = value;
    }

    public static ComplianceStatus From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ComplianceStatus cannot be empty or null", nameof(value));

        var lowerValue = value.ToLowerInvariant();
        if (!_validStatuses.TryGetValue(lowerValue, out var status))
            throw new ArgumentException($"Invalid ComplianceStatus '{value}'. Valid values are: 'taken', 'skipped'", nameof(value));

        return status;
    }

    public bool IsTaken => this.Value == "taken";
    public bool IsSkipped => this.Value == "skipped";

    public override bool Equals(object? obj)
    {
        return obj is ComplianceStatus cs && cs.Value == Value;
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
    public static implicit operator string(ComplianceStatus status) => status.Value;

    // Implicit conversion from string for convenience (factory method preferred)
    public static implicit operator ComplianceStatus(string value) => From(value);
}

