using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class SlaWindowTests
{
    private static readonly SlaPolicy Policy = SlaPolicy.Default;

    [Theory]
    [InlineData(TicketPriority.Critical, CustomerTier.Standard, 4)]
    [InlineData(TicketPriority.Critical, CustomerTier.Premium, 2)]
    [InlineData(TicketPriority.High, CustomerTier.Standard, 8)]
    [InlineData(TicketPriority.High, CustomerTier.Premium, 4)]
    [InlineData(TicketPriority.Medium, CustomerTier.Standard, 24)]
    [InlineData(TicketPriority.Medium, CustomerTier.Premium, 12)]
    [InlineData(TicketPriority.Low, CustomerTier.Standard, 72)]
    [InlineData(TicketPriority.Low, CustomerTier.Premium, 36)]
    public void Calculate_AppliesTheBaseWindowAndHalvesItForPremium(
        TicketPriority priority,
        CustomerTier tier,
        double hours)
    {
        var window = SlaWindow.Calculate(priority, tier, Policy);

        Assert.Equal(TimeSpan.FromHours(hours), window.Duration);
        Assert.False(window.WasRaisedToMinimum);
    }

    [Fact]
    public void Calculate_NeverGoesBelowTheConfiguredFloor()
    {
        var policy = Policy with
        {
            BaseWindowHours = new Dictionary<TicketPriority, double>
            {
                [TicketPriority.Low] = 72,
                [TicketPriority.Medium] = 24,
                [TicketPriority.High] = 8,
                [TicketPriority.Critical] = 1
            }
        };

        var window = SlaWindow.Calculate(TicketPriority.Critical, CustomerTier.Premium, policy);

        Assert.Equal(TimeSpan.FromHours(1), window.Duration);
        Assert.True(window.WasRaisedToMinimum);
        Assert.Contains("minimum", window.Describe(TicketPriority.Critical, CustomerTier.Premium, policy), StringComparison.OrdinalIgnoreCase);
    }
}
