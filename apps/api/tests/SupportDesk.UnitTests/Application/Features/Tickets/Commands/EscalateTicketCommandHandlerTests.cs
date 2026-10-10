using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Exceptions;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;

public class EscalateTicketCommandHandlerTests
{
    private static readonly EscalateTicketRequest Request = new("The customer is now completely blocked.");
    private const string Actor = "Alex Turner";

    [Fact]
    public async Task HandleAsync_RaisesPriorityOneStepAndRecomputesTheDueDateFromNow()
    {
        var context = Context();
        var ticket = context.ExistingTicket(new TicketBuilder().WithPriority(TicketPriority.Low).Build());
        context.Clock.Advance(TimeSpan.FromHours(3));

        await context.Handler.HandleAsync(ticket.Id, Request, CancellationToken.None);

        Assert.Equal(TicketPriority.Medium, ticket.Priority);
        Assert.Equal(context.Clock.UtcNow.AddHours(24), ticket.DueAtUtc);
        context.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task HandleAsync_KeepsTheCurrentAgentWhenTheyAreStillEligible()
    {
        var context = Context(Candidate(4, "Sara", open: 2, max: 6));
        var ticket = context.ExistingTicket(new TicketBuilder().WithPriority(TicketPriority.High).AssignedTo(4).Build());

        await context.Handler.HandleAsync(ticket.Id, Request, CancellationToken.None);

        Assert.Equal(4, ticket.AssignedAgentId);
        Assert.Equal(4, context.Recorded!.FromAgentId);
        Assert.Equal(4, context.Recorded.ToAgentId);
    }

    [Fact]
    public async Task HandleAsync_ReassignsWhenTheCurrentAgentIsNoLongerEligible()
    {
        var context = Context(
            Candidate(4, "Sara", open: 1, max: 6, active: false),
            Candidate(2, "Ben", open: 1, max: 8));
        var ticket = context.ExistingTicket(new TicketBuilder().WithPriority(TicketPriority.Medium).AssignedTo(4).Build());

        await context.Handler.HandleAsync(ticket.Id, Request, CancellationToken.None);

        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(2, ticket.AssignedAgentId);
        Assert.Equal(4, context.Recorded!.FromAgentId);
        Assert.Equal(2, context.Recorded.ToAgentId);
        Assert.Equal(Request.Reason, context.Recorded.Reason);
        Assert.Equal(Actor, context.Recorded.EscalatedBy);
        Assert.Equal(context.Clock.UtcNow, context.Recorded.EscalatedAtUtc);
        Assert.Equal(TicketPriority.Medium, context.Recorded.FromPriority);
        Assert.Equal(TicketPriority.High, context.Recorded.ToPriority);
    }

    [Fact]
    public async Task HandleAsync_WhenCritical_RejectsAndSaysSo()
    {
        var context = Context();
        var ticket = context.ExistingTicket(new TicketBuilder().WithPriority(TicketPriority.Critical).Build());

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => context.Handler.HandleAsync(ticket.Id, Request, CancellationToken.None));

        Assert.Contains("Critical", exception.Message);
        context.VerifySaved(Times.Never());
    }

    [Theory]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed)]
    public async Task HandleAsync_WhenFinished_RejectsAndNamesTheStatus(TicketStatus status)
    {
        var context = Context();
        var ticket = context.ExistingTicket(new TicketBuilder().WithStatus(status).Build());

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => context.Handler.HandleAsync(ticket.Id, Request, CancellationToken.None));

        Assert.Contains(status.ToString(), exception.Message);
        context.VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Validator_RejectsAShortReason()
    {
        var validator = new EscalateTicketRequestValidator();

        var result = await validator.ValidateAsync(new EscalateTicketRequest("no"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(EscalateTicketRequest.Reason));
    }

    private static EscalationContext Context(params AgentCandidate[] agents)
    {
        var commands = new TicketCommandTestContext();
        commands.Agents
            .Setup(agent => agent.ListCandidatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(agents);
        TicketEscalation? recorded = null;
        commands.Tickets
            .Setup(ticket => ticket.AddEscalationAsync(It.IsAny<TicketEscalation>(), It.IsAny<CancellationToken>()))
            .Callback<TicketEscalation, CancellationToken>((escalation, _) => recorded = escalation)
            .Returns(Task.CompletedTask);

        var handler = new EscalateTicketCommandHandler(
            commands.Tickets.Object,
            commands.Customers.Object,
            commands.Categories.Object,
            commands.Agents.Object,
            commands.UnitOfWork.Object,
            commands.Clock,
            new FixedAgent(Actor),
            new GetTicketQueryHandler(commands.TicketQueries.Object),
            new TicketTriage(SlaPolicy.Default));

        return new EscalationContext(commands, handler, () => recorded);
    }

    private sealed class FixedAgent(string fullName) : ICurrentAgent
    {
        public int Id => 1;

        public string FullName => fullName;
    }

    private static AgentCandidate Candidate(int id, string name, int open, int max, bool active = true) =>
        new(id, name, active, max, open, new HashSet<int>());

    private sealed class EscalationContext
    {
        private readonly Func<TicketEscalation?> _recorded;

        public EscalationContext(TicketCommandTestContext commands, EscalateTicketCommandHandler handler, Func<TicketEscalation?> recorded)
        {
            Commands = commands;
            Handler = handler;
            _recorded = recorded;
        }

        public TicketCommandTestContext Commands { get; }

        public EscalateTicketCommandHandler Handler { get; }

        public FixedClock Clock => Commands.Clock;

        public TicketEscalation? Recorded => _recorded();

        public Ticket ExistingTicket(Ticket ticket) => Commands.ExistingTicket(ticket);

        public void VerifySaved(Times times) => Commands.VerifySaved(times);
    }
}
