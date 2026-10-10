using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;

namespace SupportDesk.Application.Features.Tickets.Queries.GetEscalations;

/// <summary>Reads a ticket's escalation history, newest first.</summary>
public sealed class GetEscalationsQueryHandler(ITicketQueries tickets)
{
    public Task<IReadOnlyList<TicketEscalationDto>> HandleAsync(int ticketId, CancellationToken ct) =>
        tickets.GetEscalationsAsync(ticketId, ct);
}