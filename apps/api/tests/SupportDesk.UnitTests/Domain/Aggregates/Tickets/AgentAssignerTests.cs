using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class AgentAssignerTests
{
    private const int CategoryId = 4;

    [Fact]
    public void Select_ChoosesTheFewestOpenTickets()
    {
        var decision = AgentAssigner.Select(
            [
                Candidate(1, "Alex", open: 5, max: 10),
                Candidate(2, "Ben", open: 1, max: 8),
                Candidate(3, "Priya", open: 3, max: 8)
            ],
            requiresSpecialist: false,
            CategoryId);

        Assert.Equal(2, decision.AgentId);
        Assert.Contains("Ben", decision.Reason);
    }

    [Fact]
    public void Select_WhenOpenCountsTie_ChoosesTheLowestId()
    {
        var decision = AgentAssigner.Select(
            [
                Candidate(7, "Later", open: 2, max: 8),
                Candidate(3, "Earlier", open: 2, max: 8)
            ],
            requiresSpecialist: false,
            CategoryId);

        Assert.Equal(3, decision.AgentId);
        Assert.Contains("lowest agent id", decision.Reason);
    }

    [Fact]
    public void Select_WhenASpecialistIsRequired_IgnoresAgentsWithoutThatCategory()
    {
        var decision = AgentAssigner.Select(
            [
                Candidate(1, "Generalist", open: 0, max: 10, categories: [1]),
                Candidate(2, "Specialist", open: 4, max: 8, categories: [CategoryId])
            ],
            requiresSpecialist: true,
            CategoryId);

        Assert.Equal(2, decision.AgentId);
    }

    [Fact]
    public void Select_SkipsAgentsAtTheirLimitAndInactiveAgents()
    {
        var decision = AgentAssigner.Select(
            [
                Candidate(1, "Full", open: 6, max: 6),
                Candidate(2, "Inactive", open: 0, max: 8, active: false),
                Candidate(3, "Available", open: 4, max: 8)
            ],
            requiresSpecialist: false,
            CategoryId);

        Assert.Equal(3, decision.AgentId);
    }

    [Fact]
    public void Select_WhenNobodyIsEligible_LeavesTheTicketUnassigned()
    {
        var decision = AgentAssigner.Select(
            [Candidate(1, "Full", open: 6, max: 6, categories: [CategoryId])],
            requiresSpecialist: true,
            CategoryId);

        Assert.Null(decision.AgentId);
        Assert.Contains("Unassigned", decision.Reason);
    }

    [Fact]
    public void Select_KeepsTheCurrentAgentWhenTheyAreStillEligible()
    {
        var decision = AgentAssigner.Select(
            [
                Candidate(1, "Alex", open: 6, max: 6),
                Candidate(2, "Ben", open: 0, max: 8)
            ],
            requiresSpecialist: false,
            CategoryId,
            preferAgentId: 1);

        Assert.Equal(1, decision.AgentId);
        Assert.Contains("Kept Alex", decision.Reason);
    }

    [Fact]
    public void Select_ReplacesTheCurrentAgentWhenTheyAreNoLongerEligible()
    {
        var decision = AgentAssigner.Select(
            [
                Candidate(1, "Alex", open: 1, max: 8, active: false),
                Candidate(2, "Ben", open: 3, max: 8)
            ],
            requiresSpecialist: false,
            CategoryId,
            preferAgentId: 1);

        Assert.Equal(2, decision.AgentId);
        Assert.Contains("no longer eligible", decision.Reason);
    }

    private static AgentCandidate Candidate(
        int id,
        string name,
        int open,
        int max,
        bool active = true,
        int[]? categories = null) =>
        new(id, name, active, max, open, (categories ?? []).ToHashSet());
}
