using SupportDesk.Domain.Exceptions;

namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// How a ticket's priority is chosen on creation, and how far an escalation may move it.
/// Category names are never consulted: only the category's flags.
/// </summary>
public static class TicketPriorityRules
{
    public static PriorityDecision Resolve(TicketPriority? requested, bool forcesCriticalPriority)
    {
        if (forcesCriticalPriority)
        {
            return new PriorityDecision(
                TicketPriority.Critical,
                "This category forces Critical priority.");
        }

        if (requested is null)
        {
            return new PriorityDecision(TicketPriority.Medium, "No priority was requested, so Medium was applied.");
        }

        return new PriorityDecision(requested.Value, $"The requested priority {requested.Value} was applied.");
    }

    /// <summary>The next priority up. Critical cannot be raised.</summary>
    /// <exception cref="BusinessRuleViolationException">The ticket is already Critical.</exception>
    public static TicketPriority Raise(TicketPriority current, string reference)
    {
        var next = current switch
        {
            TicketPriority.Low => TicketPriority.Medium,
            TicketPriority.Medium => TicketPriority.High,
            TicketPriority.High => TicketPriority.Critical,
            _ => (TicketPriority?)null
        };

        if (next is null)
        {
            throw new BusinessRuleViolationException(
                $"Ticket {reference} is Critical and cannot be escalated.");
        }

        return next.Value;
    }
}

/// <summary>The priority that was applied, and a sentence saying why.</summary>
public sealed record PriorityDecision(TicketPriority Priority, string Reason);
