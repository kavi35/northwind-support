using FluentValidation;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketRequestValidator : AbstractValidator<EscalateTicketRequest>
{
    public EscalateTicketRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MinimumLength(TicketEscalation.ReasonMinLength)
            .MaximumLength(TicketEscalation.ReasonMaxLength);
    }
}
