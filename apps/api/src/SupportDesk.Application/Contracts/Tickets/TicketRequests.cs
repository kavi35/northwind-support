using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>Body of POST /api/tickets.</summary>
public sealed record CreateTicketRequest(
    string Title,
    string Description,
    int CustomerId,
    int CategoryId,
    TicketPriority? RequestedPriority);

/// <summary>Body of PATCH /api/tickets/{id}/status.</summary>
public sealed record UpdateTicketStatusRequest(TicketStatus Status);

/// <summary>Body of PATCH /api/tickets/{id}/assignment. A null agent unassigns the ticket.</summary>
public sealed record AssignTicketRequest(int? AgentId);

/// <summary>Body of POST /api/tickets/{id}/escalate. Who escalated it comes from the token.</summary>
public sealed record EscalateTicketRequest(string Reason);

/// <summary>Why the priority, the window and the owner came out the way they did.</summary>
public sealed record TriageDto(string PriorityReason, string SlaWindowReason, string AssignmentReason);

/// <summary>One row of the escalation history.</summary>
public sealed record TicketEscalationDto(
    int Id,
    TicketPriority FromPriority,
    TicketPriority ToPriority,
    AgentSummaryDto? FromAgent,
    AgentSummaryDto? ToAgent,
    DateTime? FromDueAtUtc,
    DateTime? ToDueAtUtc,
    string Reason,
    string EscalatedBy,
    DateTime EscalatedAtUtc);

/// <summary>Body of POST /api/tickets/{id}/escalate.</summary>
public sealed record EscalateTicketResponse(TicketDetailDto Ticket, TicketEscalationDto Escalation);
