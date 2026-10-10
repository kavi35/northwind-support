using SupportDesk.Domain.Aggregates.Customers;

namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// How long a ticket is given, from the moment the window starts. Premium customers get a
/// shorter window, never below the configured floor.
/// </summary>
public readonly record struct SlaWindow(TimeSpan Duration, bool WasRaisedToMinimum)
{
    public static SlaWindow Calculate(TicketPriority priority, CustomerTier tier, SlaPolicy policy)
    {
        if (!policy.BaseWindowHours.TryGetValue(priority, out var baseHours))
        {
            throw new InvalidOperationException($"No SLA window is configured for {priority}.");
        }

        var hours = tier == CustomerTier.Premium
            ? baseHours * policy.PremiumCustomerMultiplier
            : baseHours;

        var raised = hours < policy.MinimumWindowHours;
        if (raised)
        {
            hours = policy.MinimumWindowHours;
        }

        return new SlaWindow(TimeSpan.FromHours(hours), raised);
    }

    /// <summary>
    /// How much of the window may be left before the ticket is at risk. Shared with the SQL
    /// filter so the two cannot drift.
    /// </summary>
    public TimeSpan AtRiskAllowance(double atRiskThresholdPercent) =>
        TimeSpan.FromTicks((long)(Duration.Ticks * (atRiskThresholdPercent / 100d)));

    public string Describe(TicketPriority priority, CustomerTier tier, SlaPolicy policy)
    {
        var baseHours = policy.BaseWindowHours[priority];
        var applied = Duration.TotalHours;

        if (tier == CustomerTier.Premium)
        {
            var text =
                $"Premium customers receive {policy.PremiumCustomerMultiplier:0.##} times the {baseHours:0.##} hour {priority} window, so this window is {applied:0.##} hours.";

            if (WasRaisedToMinimum)
            {
                text += $" It was raised to the {policy.MinimumWindowHours:0.##} hour minimum.";
            }

            return text;
        }

        return $"The {priority} window is {applied:0.##} hours.";
    }
}
