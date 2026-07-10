using MediTrack.FollowUpService.API.Domain.Model.ValueObjects;
using Xunit;

namespace MediTrack.FollowUpService.Tests;

/// Cubre: "cálculo de adherencia usando únicamente Approved" — el cálculo real
/// (AdherenceHistoryQueryService) delega en ComplianceStatus.IsTaken, así que
/// probar el VO directamente es la forma más precisa de verificar esta regla
/// sin duplicar toda la infraestructura de la query.
public class ComplianceStatusTests
{
    [Theory]
    [InlineData("taken", true)]
    [InlineData("approved", true)]
    [InlineData("skipped", false)]
    [InlineData("pendingvalidation", false)]
    [InlineData("rejected", false)]
    public void IsTaken_SoloCuentaTakenLegadoYApproved(string status, bool expected)
    {
        var result = ComplianceStatus.From(status);
        Assert.Equal(expected, result.IsTaken);
    }

    [Fact]
    public void PendingValidation_NoEsApprovedNiRejected()
    {
        var status = ComplianceStatus.From("pendingvalidation");
        Assert.True(status.IsPendingValidation);
        Assert.False(status.IsApproved);
        Assert.False(status.IsRejected);
    }

    [Fact]
    public void Rejected_NoCuentaComoTomada()
    {
        var status = ComplianceStatus.From("rejected");
        Assert.True(status.IsRejected);
        Assert.False(status.IsTaken);
    }

    [Fact]
    public void From_ValorInvalido_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ComplianceStatus.From("invalido"));
    }
}
