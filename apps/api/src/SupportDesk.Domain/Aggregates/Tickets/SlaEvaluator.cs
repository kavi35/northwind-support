namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out where a ticket stands against its due date. SLA status is always derived from
/// the ticket's dates and the current time - never stored. "Now" is passed in.
/// </summary>
public static class SlaEvaluator
{
    public static SlaStatus Evaluate(
        DateTime? dueAtUtc,
        DateTime? resolvedAtUtc,
        DateTime nowUtc,
        TimeSpan window,
        double atRiskThresholdPercent)
    {
        if (dueAtUtc is null)
        {
            return SlaStatus.NotApplicable;
        }

        if (resolvedAtUtc is not null)
        {
            return resolvedAtUtc <= dueAtUtc ? SlaStatus.Met : SlaStatus.Breached;
        }

        if (nowUtc > dueAtUtc)
        {
            return SlaStatus.Breached;
        }

        var allowance = new SlaWindow(window, WasRaisedToMinimum: false).AtRiskAllowance(atRiskThresholdPercent);
        var remaining = dueAtUtc.Value - nowUtc;

        return remaining <= allowance ? SlaStatus.AtRisk : SlaStatus.WithinSla;
    }
}
