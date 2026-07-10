namespace MediTrack.FollowUpService.API.Domain.Model.ValueObjects;

public class ComplianceStatus
{
    public static readonly ComplianceStatus Taken = new("taken");
    public static readonly ComplianceStatus Skipped = new("skipped");

    /// Flujo de validación de evidencia en video (MediTrack AI Validator — Prototype):
    /// el paciente sube video -> PendingValidation -> el validador humano decide -> Approved/Rejected.
    public static readonly ComplianceStatus PendingValidation = new("pendingvalidation");
    public static readonly ComplianceStatus Approved = new("approved");
    public static readonly ComplianceStatus Rejected = new("rejected");

    private static readonly Dictionary<string, ComplianceStatus> _validStatuses = new()
    {
        { "taken", Taken },
        { "skipped", Skipped },
        { "pendingvalidation", PendingValidation },
        { "approved", Approved },
        { "rejected", Rejected }
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
            throw new ArgumentException(
                $"Invalid ComplianceStatus '{value}'. Valid values are: 'taken', 'skipped', 'pendingvalidation', 'approved', 'rejected'",
                nameof(value));

        return status;
    }

    // "Taken" (registro directo legado) y "Approved" (validado por evidencia) cuentan
    // ambos como dosis cumplida para adherencia y para excluir la dosis de "next dose".
    public bool IsTaken => Value == "taken" || Value == "approved";
    public bool IsSkipped => Value == "skipped";
    public bool IsPendingValidation => Value == "pendingvalidation";
    public bool IsApproved => Value == "approved";
    public bool IsRejected => Value == "rejected";

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
