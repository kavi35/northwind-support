using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Triage;

/// <summary>
/// Decides priority, due date and owner. Create and escalate both go through here, so the
/// rules are not copied into either handler.
/// </summary>
public sealed class TicketTriage(SlaPolicy policy)
{
    public TriageOutcome ForNewTicket(
        TicketPriority? requested,
        bool forcesCriticalPriority,
        bool requiresSpecialist,
        int categoryId,
        CustomerTier tier,
        IReadOnlyList<AgentCandidate> agents,
        DateTime nowUtc)
    {
        var priority = TicketPriorityRules.Resolve(requested, forcesCriticalPriority);
        var window = SlaWindow.Calculate(priority.Priority, tier, policy);
        var assignment = AgentAssigner.Select(agents, requiresSpecialist, categoryId);

        return new TriageOutcome(
            priority.Priority,
            priority.Reason,
            nowUtc.Add(window.Duration),
            window.Describe(priority.Priority, tier, policy),
            assignment.AgentId,
            assignment.Reason);
    }

    public TriageOutcome ForEscalation(
        TicketPriority current,
        string reference,
        bool requiresSpecialist,
        int categoryId,
        CustomerTier tier,
        IReadOnlyList<AgentCandidate> agents,
        int? currentAgentId,
        DateTime nowUtc)
    {
        var raised = TicketPriorityRules.Raise(current, reference);
        var window = SlaWindow.Calculate(raised, tier, policy);
        var assignment = AgentAssigner.Select(agents, requiresSpecialist, categoryId, currentAgentId);

        return new TriageOutcome(
            raised,
            $"Escalated from {current} to {raised}.",
            nowUtc.Add(window.Duration),
            window.Describe(raised, tier, policy),
            assignment.AgentId,
            assignment.Reason);
    }
}

/// <summary>The decision create and escalate both return to the caller.</summary>
public sealed record TriageOutcome(
    TicketPriority Priority,
    string PriorityReason,
    DateTime DueAtUtc,
    string SlaWindowReason,
    int? AgentId,
    string AssignmentReason);
