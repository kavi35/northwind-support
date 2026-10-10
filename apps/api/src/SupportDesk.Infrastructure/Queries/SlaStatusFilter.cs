using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Infrastructure.Queries;

/// <summary>
/// Turns a derived SLA status into a SQL predicate. The allowance for each priority and tier
/// comes from <see cref="SlaWindow"/>, the same calculation the in-memory evaluator uses.
/// </summary>
internal static class SlaStatusFilter
{
    public static IQueryable<TicketQueries.TicketWithLabels> WhereStatus(
        IQueryable<TicketQueries.TicketWithLabels> source,
        SlaStatus status,
        DateTime nowUtc,
        SlaPolicy policy)
    {
        var limits = CalculateLimits(nowUtc, policy);

        return status switch
        {
            SlaStatus.NotApplicable => source.Where(x => x.Ticket.DueAtUtc == null),

            SlaStatus.Met => source.Where(x =>
                x.Ticket.DueAtUtc != null
                && x.Ticket.ResolvedAtUtc != null
                && x.Ticket.ResolvedAtUtc <= x.Ticket.DueAtUtc),

            SlaStatus.Breached => source.Where(x =>
                x.Ticket.DueAtUtc != null
                && ((x.Ticket.ResolvedAtUtc != null && x.Ticket.ResolvedAtUtc > x.Ticket.DueAtUtc)
                    || (x.Ticket.ResolvedAtUtc == null && x.Ticket.DueAtUtc < nowUtc))),

            SlaStatus.AtRisk => source.Where(x =>
                x.Ticket.DueAtUtc != null
                && x.Ticket.ResolvedAtUtc == null
                && x.Ticket.DueAtUtc >= nowUtc
                && (
                    (x.Ticket.Priority == TicketPriority.Low && x.Customer.Tier == CustomerTier.Standard && x.Ticket.DueAtUtc <= limits.LowStandard)
                    || (x.Ticket.Priority == TicketPriority.Low && x.Customer.Tier == CustomerTier.Premium && x.Ticket.DueAtUtc <= limits.LowPremium)
                    || (x.Ticket.Priority == TicketPriority.Medium && x.Customer.Tier == CustomerTier.Standard && x.Ticket.DueAtUtc <= limits.MediumStandard)
                    || (x.Ticket.Priority == TicketPriority.Medium && x.Customer.Tier == CustomerTier.Premium && x.Ticket.DueAtUtc <= limits.MediumPremium)
                    || (x.Ticket.Priority == TicketPriority.High && x.Customer.Tier == CustomerTier.Standard && x.Ticket.DueAtUtc <= limits.HighStandard)
                    || (x.Ticket.Priority == TicketPriority.High && x.Customer.Tier == CustomerTier.Premium && x.Ticket.DueAtUtc <= limits.HighPremium)
                    || (x.Ticket.Priority == TicketPriority.Critical && x.Customer.Tier == CustomerTier.Standard && x.Ticket.DueAtUtc <= limits.CriticalStandard)
                    || (x.Ticket.Priority == TicketPriority.Critical && x.Customer.Tier == CustomerTier.Premium && x.Ticket.DueAtUtc <= limits.CriticalPremium))),

            SlaStatus.WithinSla => source.Where(x =>
                x.Ticket.DueAtUtc != null
                && x.Ticket.ResolvedAtUtc == null
                && (
                    (x.Ticket.Priority == TicketPriority.Low && x.Customer.Tier == CustomerTier.Standard && x.Ticket.DueAtUtc > limits.LowStandard)
                    || (x.Ticket.Priority == TicketPriority.Low && x.Customer.Tier == CustomerTier.Premium && x.Ticket.DueAtUtc > limits.LowPremium)
                    || (x.Ticket.Priority == TicketPriority.Medium && x.Customer.Tier == CustomerTier.Standard && x.Ticket.DueAtUtc > limits.MediumStandard)
                    || (x.Ticket.Priority == TicketPriority.Medium && x.Customer.Tier == CustomerTier.Premium && x.Ticket.DueAtUtc > limits.MediumPremium)
                    || (x.Ticket.Priority == TicketPriority.High && x.Customer.Tier == CustomerTier.Standard && x.Ticket.DueAtUtc > limits.HighStandard)
                    || (x.Ticket.Priority == TicketPriority.High && x.Customer.Tier == CustomerTier.Premium && x.Ticket.DueAtUtc > limits.HighPremium)
                    || (x.Ticket.Priority == TicketPriority.Critical && x.Customer.Tier == CustomerTier.Standard && x.Ticket.DueAtUtc > limits.CriticalStandard)
                    || (x.Ticket.Priority == TicketPriority.Critical && x.Customer.Tier == CustomerTier.Premium && x.Ticket.DueAtUtc > limits.CriticalPremium))),

            _ => source
        };
    }

    private static WindowLimits CalculateLimits(DateTime nowUtc, SlaPolicy policy) => new(
        Limit(nowUtc, TicketPriority.Low, CustomerTier.Standard, policy),
        Limit(nowUtc, TicketPriority.Low, CustomerTier.Premium, policy),
        Limit(nowUtc, TicketPriority.Medium, CustomerTier.Standard, policy),
        Limit(nowUtc, TicketPriority.Medium, CustomerTier.Premium, policy),
        Limit(nowUtc, TicketPriority.High, CustomerTier.Standard, policy),
        Limit(nowUtc, TicketPriority.High, CustomerTier.Premium, policy),
        Limit(nowUtc, TicketPriority.Critical, CustomerTier.Standard, policy),
        Limit(nowUtc, TicketPriority.Critical, CustomerTier.Premium, policy));

    private static DateTime Limit(DateTime nowUtc, TicketPriority priority, CustomerTier tier, SlaPolicy policy)
    {
        var window = SlaWindow.Calculate(priority, tier, policy);
        return nowUtc.Add(window.AtRiskAllowance(policy.AtRiskThresholdPercent));
    }

    private sealed record WindowLimits(
        DateTime LowStandard,
        DateTime LowPremium,
        DateTime MediumStandard,
        DateTime MediumPremium,
        DateTime HighStandard,
        DateTime HighPremium,
        DateTime CriticalStandard,
        DateTime CriticalPremium);
}
