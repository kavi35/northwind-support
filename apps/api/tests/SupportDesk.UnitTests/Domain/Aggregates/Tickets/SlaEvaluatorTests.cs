using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class SlaEvaluatorTests
{
    private static readonly DateTime Now = FixedClock.DefaultNow;
    private static readonly TimeSpan Window = TimeSpan.FromHours(4);
    private const double Threshold = 25;

    [Fact]
    public void Evaluate_WithoutADueDate_IsNotApplicable() =>
        Assert.Equal(SlaStatus.NotApplicable, SlaEvaluator.Evaluate(null, null, Now, Window, Threshold));

    [Fact]
    public void Evaluate_ResolvedOnTime_IsMet() =>
        Assert.Equal(SlaStatus.Met, SlaEvaluator.Evaluate(Now, Now, Now.AddDays(1), Window, Threshold));

    [Fact]
    public void Evaluate_ResolvedLate_IsBreached() =>
        Assert.Equal(SlaStatus.Breached, SlaEvaluator.Evaluate(Now, Now.AddMinutes(1), Now, Window, Threshold));

    [Fact]
    public void Evaluate_OpenAndPastDue_IsBreached() =>
        Assert.Equal(SlaStatus.Breached, SlaEvaluator.Evaluate(Now.AddMinutes(-1), null, Now, Window, Threshold));

    [Fact]
    public void Evaluate_OpenAndComfortablyBeforeDue_IsWithinSla() =>
        Assert.Equal(SlaStatus.WithinSla, SlaEvaluator.Evaluate(Now.AddHours(4), null, Now, Window, Threshold));

    [Fact]
    public void Evaluate_WithExactlyAQuarterOfTheWindowLeft_IsAtRisk() =>
        Assert.Equal(SlaStatus.AtRisk, SlaEvaluator.Evaluate(Now.AddHours(1), null, Now, Window, Threshold));

    [Fact]
    public void Evaluate_JustAboveTheAtRiskBoundary_IsWithinSla() =>
        Assert.Equal(
            SlaStatus.WithinSla,
            SlaEvaluator.Evaluate(Now.AddHours(1).AddTicks(1), null, Now, Window, Threshold));

    [Fact]
    public void Evaluate_AtTheExactDueInstant_IsAtRisk() =>
        Assert.Equal(SlaStatus.AtRisk, SlaEvaluator.Evaluate(Now, null, Now, Window, Threshold));

    [Fact]
    public void Evaluate_OneTickAfterDue_IsBreached() =>
        Assert.Equal(SlaStatus.Breached, SlaEvaluator.Evaluate(Now.AddTicks(-1), null, Now, Window, Threshold));
}
