namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Picks an owner. Eligible agents are active, specialised when the category requires it,
/// and strictly below their open-ticket limit. The fewest open tickets wins. A tie goes to
/// the lowest agent id, so the same inputs always pick the same agent.
/// </summary>
public static class AgentAssigner
{
    /// <param name="preferAgentId">
    /// When set, that agent is kept if they are still eligible. The ticket they already hold
    /// is not counted against their limit, so being full because of this ticket does not
    /// push it off them.
    /// </param>
    public static AssignmentDecision Select(
        IReadOnlyList<AgentCandidate> agents,
        bool requiresSpecialist,
        int categoryId,
        int? preferAgentId = null)
    {
        if (preferAgentId is int currentId)
        {
            var current = agents.FirstOrDefault(agent => agent.Id == currentId);
            if (current is not null && IsEligible(current, requiresSpecialist, categoryId, OpenCount(current, holdingThisTicket: true)))
            {
                return new AssignmentDecision(current.Id, $"Kept {current.FullName}, who is still eligible for this ticket.");
            }
        }

        var eligible = agents
            .Where(agent => IsEligible(agent, requiresSpecialist, categoryId, agent.OpenTicketCount))
            .OrderBy(agent => agent.OpenTicketCount)
            .ThenBy(agent => agent.Id)
            .ToList();

        if (eligible.Count == 0)
        {
            var reason = requiresSpecialist
                ? "Unassigned because no active specialist for this category is under their open-ticket limit."
                : "Unassigned because no active agent is under their open-ticket limit.";

            if (preferAgentId is not null)
            {
                reason = "The current agent is no longer eligible. " + reason;
            }

            return new AssignmentDecision(null, reason);
        }

        var chosen = eligible[0];
        var tied = eligible.Skip(1).Any(agent => agent.OpenTicketCount == chosen.OpenTicketCount);
        var reasonText =
            $"Assigned to {chosen.FullName}, who has the fewest open tickets ({chosen.OpenTicketCount}).";

        if (tied)
        {
            reasonText += " Several agents had the same number, so the lowest agent id was chosen.";
        }

        if (preferAgentId is not null)
        {
            reasonText = "The current agent is no longer eligible. " + reasonText;
        }

        return new AssignmentDecision(chosen.Id, reasonText);
    }

    private static int OpenCount(AgentCandidate agent, bool holdingThisTicket) =>
        holdingThisTicket ? Math.Max(0, agent.OpenTicketCount - 1) : agent.OpenTicketCount;

    private static bool IsEligible(AgentCandidate agent, bool requiresSpecialist, int categoryId, int openTickets) =>
        agent.IsActive
        && openTickets < agent.MaxOpenTickets
        && (!requiresSpecialist || agent.SpecializationCategoryIds.Contains(categoryId));
}

/// <summary>Who should own the ticket, and a sentence saying why. A null agent is a normal outcome.</summary>
public sealed record AssignmentDecision(int? AgentId, string Reason);
