using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Exceptions;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class TicketPriorityRulesTests
{
    [Fact]
    public void Resolve_WithoutARequest_IsMedium()
    {
        var decision = TicketPriorityRules.Resolve(requested: null, forcesCriticalPriority: false);

        Assert.Equal(TicketPriority.Medium, decision.Priority);
    }

    [Fact]
    public void Resolve_UsesTheRequestedPriority()
    {
        var decision = TicketPriorityRules.Resolve(TicketPriority.High, forcesCriticalPriority: false);

        Assert.Equal(TicketPriority.High, decision.Priority);
    }

    [Theory]
    [InlineData(TicketPriority.Low)]
    [InlineData(TicketPriority.Medium)]
    [InlineData(TicketPriority.High)]
    [InlineData(null)]
    public void Resolve_WhenTheCategoryForcesCritical_IsAlwaysCritical(TicketPriority? requested)
    {
        var decision = TicketPriorityRules.Resolve(requested, forcesCriticalPriority: true);

        Assert.Equal(TicketPriority.Critical, decision.Priority);
        Assert.Contains("forces Critical", decision.Reason);
        Assert.DoesNotContain("Security", decision.Reason);
        Assert.DoesNotContain("Outage", decision.Reason);
    }

    [Theory]
    [InlineData(TicketPriority.Low, TicketPriority.Medium)]
    [InlineData(TicketPriority.Medium, TicketPriority.High)]
    [InlineData(TicketPriority.High, TicketPriority.Critical)]
    public void Raise_MovesUpOneLevel(TicketPriority current, TicketPriority expected) =>
        Assert.Equal(expected, TicketPriorityRules.Raise(current, "TCK-0001"));

    [Fact]
    public void Raise_WhenCritical_SaysSo()
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(
            () => TicketPriorityRules.Raise(TicketPriority.Critical, "TCK-0001"));

        Assert.Contains("Critical", exception.Message);
    }
}
