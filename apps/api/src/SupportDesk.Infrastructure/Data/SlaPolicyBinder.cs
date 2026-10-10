using System.Globalization;
using Microsoft.Extensions.Configuration;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Infrastructure.Data;

/// <summary>
/// Reads the <c>Sla</c> configuration section. Missing or unreadable numbers fail at startup
/// rather than silently using a hard-coded window.
/// </summary>
public static class SlaPolicyBinder
{
    public const string SectionName = "Sla";

    public static SlaPolicy Bind(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var windows = new Dictionary<TicketPriority, double>();

        foreach (var priority in Enum.GetValues<TicketPriority>())
        {
            var raw = section.GetSection("BaseWindowHours")[priority.ToString()];
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var hours) || hours <= 0)
            {
                throw new InvalidOperationException(
                    $"Configuration '{SectionName}:BaseWindowHours:{priority}' must be a positive number of hours.");
            }

            windows[priority] = hours;
        }

        return new SlaPolicy(
            windows,
            ReadPositive(section, "PremiumCustomerMultiplier"),
            ReadPositive(section, "MinimumWindowHours"),
            ReadPositive(section, "AtRiskThresholdPercent"));
    }

    private static double ReadPositive(IConfiguration section, string key)
    {
        var raw = section[key];
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            throw new InvalidOperationException(
                $"Configuration '{SectionName}:{key}' must be a positive number.");
        }

        return value;
    }
}
