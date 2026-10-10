namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// What the assigner needs to know about an agent. Open tickets are those that are not
/// Resolved or Closed, counted by the caller.
/// </summary>
public sealed record AgentCandidate(
    int Id,
    string FullName,
    bool IsActive,
    int MaxOpenTickets,
    int OpenTicketCount,
    IReadOnlySet<int> SpecializationCategoryIds);
