using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

/// <summary>
/// Raises a ticket one priority, recomputes its due date from now, and keeps or replaces the owner.
/// </summary>
public sealed class EscalateTicketCommandHandler(
    ITicketRepository tickets,
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentRepository agents,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentAgent currentAgent,
    GetTicketQueryHandler getTicket,
    TicketTriage triage)
{
    /// <exception cref="NotFoundException">The ticket, its customer or its category does not exist.</exception>
    /// <exception cref="BusinessRuleViolationException">The ticket is Critical, Resolved or Closed.</exception>
    public async Task<EscalateTicketResponse> HandleAsync(int id, EscalateTicketRequest request, CancellationToken ct)
    {
        var ticket = await tickets.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Ticket", id);

        var customer = await customers.GetByIdAsync(ticket.CustomerId, ct)
            ?? throw new NotFoundException("Customer", ticket.CustomerId);

        var category = await categories.GetByIdAsync(ticket.CategoryId, ct)
            ?? throw new NotFoundException("Category", ticket.CategoryId);

        if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            throw new BusinessRuleViolationException(
                $"Ticket {ticket.Reference} is {ticket.Status} and cannot be escalated.");
        }

        var candidates = await agents.ListCandidatesAsync(ct);
        var now = clock.UtcNow;
        var fromPriority = ticket.Priority;
        var fromAgentId = ticket.AssignedAgentId;
        var fromDue = ticket.DueAtUtc;

        var decision = triage.ForEscalation(
            fromPriority,
            ticket.Reference,
            category.RequiresSpecialist,
            category.Id,
            customer.Tier,
            candidates,
            fromAgentId,
            now);

        ticket.EscalateTo(decision.Priority, decision.DueAtUtc, decision.AgentId, now);

        var escalation = new TicketEscalation(
            ticket.Id,
            fromPriority,
            decision.Priority,
            fromAgentId,
            decision.AgentId,
            fromDue,
            decision.DueAtUtc,
            request.Reason,
            currentAgent.FullName,
            now);

        await tickets.AddEscalationAsync(escalation, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var detail = await getTicket.HandleAsync(ticket.Id, ct);
        return new EscalateTicketResponse(detail, ToDto(escalation, candidates));
    }

    private static TicketEscalationDto ToDto(TicketEscalation escalation, IReadOnlyList<AgentCandidate> candidates)
    {
        AgentSummaryDto? Agent(int? id) =>
            id is int agentId
                ? candidates
                    .Where(candidate => candidate.Id == agentId)
                    .Select(candidate => new AgentSummaryDto(candidate.Id, candidate.FullName))
                    .FirstOrDefault()
                : null;

        return new TicketEscalationDto(
            escalation.Id,
            escalation.FromPriority,
            escalation.ToPriority,
            Agent(escalation.FromAgentId),
            Agent(escalation.ToAgentId),
            escalation.FromDueAtUtc,
            escalation.ToDueAtUtc,
            escalation.Reason,
            escalation.EscalatedBy,
            escalation.EscalatedAtUtc);
    }
}
