namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// The SLA numbers the business can change without a code change. Hours are decimals so a
/// half window stays exact.
/// </summary>
public sealed record SlaPolicy(
    IReadOnlyDictionary<TicketPriority, double> BaseWindowHours,
    double PremiumCustomerMultiplier,
    double MinimumWindowHours,
    double AtRiskThresholdPercent)
{
    /// <summary>The values shipped in appsettings. Used when a test does not care which numbers.</summary>
    public static SlaPolicy Default { get; } = new(
        new Dictionary<TicketPriority, double>
        {
            [TicketPriority.Low] = 72,
            [TicketPriority.Medium] = 24,
            [TicketPriority.High] = 8,
            [TicketPriority.Critical] = 4
        },
        PremiumCustomerMultiplier: 0.5,
        MinimumWindowHours: 1,
        AtRiskThresholdPercent: 25);
}
