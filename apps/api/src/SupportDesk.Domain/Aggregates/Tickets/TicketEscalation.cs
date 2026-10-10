using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// One escalation, written once and never changed. The ticket keeps the current priority,
/// owner and due date; this row keeps what they were before.
/// </summary>
public sealed class TicketEscalation : Entity
{
    public const int ReasonMaxLength = 500;
    public const int EscalatedByMaxLength = 100;
    public const int ReasonMinLength = 5;

    private TicketEscalation()
    {
        // For EF Core.
    }

    public TicketEscalation(
        int ticketId,
        TicketPriority fromPriority,
        TicketPriority toPriority,
        int? fromAgentId,
        int? toAgentId,
        DateTime? fromDueAtUtc,
        DateTime? toDueAtUtc,
        string reason,
        string escalatedBy,
        DateTime escalatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(escalatedBy);

        var trimmedReason = reason.Trim();
        var trimmedBy = escalatedBy.Trim();

        if (trimmedReason.Length < ReasonMinLength || trimmedReason.Length > ReasonMaxLength)
        {
            throw new ArgumentException(
                $"A reason must be between {ReasonMinLength} and {ReasonMaxLength} characters.",
                nameof(reason));
        }

        if (trimmedBy.Length > EscalatedByMaxLength)
        {
            throw new ArgumentException(
                $"Escalated by must be at most {EscalatedByMaxLength} characters.",
                nameof(escalatedBy));
        }

        TicketId = ticketId;
        FromPriority = fromPriority;
        ToPriority = toPriority;
        FromAgentId = fromAgentId;
        ToAgentId = toAgentId;
        FromDueAtUtc = fromDueAtUtc;
        ToDueAtUtc = toDueAtUtc;
        Reason = trimmedReason;
        EscalatedBy = trimmedBy;
        EscalatedAtUtc = escalatedAtUtc;
    }

    public int TicketId { get; private set; }

    public TicketPriority FromPriority { get; private set; }

    public TicketPriority ToPriority { get; private set; }

    public int? FromAgentId { get; private set; }

    public int? ToAgentId { get; private set; }

    public DateTime? FromDueAtUtc { get; private set; }

    public DateTime? ToDueAtUtc { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string EscalatedBy { get; private set; } = string.Empty;

    public DateTime EscalatedAtUtc { get; private set; }
}
