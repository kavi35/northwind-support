using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Repositories;

public sealed class AgentRepository(SupportDbContext db) : IAgentRepository
{
    /// <summary>Loads the whole aggregate, specializations included.</summary>
    public Task<Agent?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Set<Agent>()
            .Include(a => a.Specializations)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AgentCandidate>> ListCandidatesAsync(CancellationToken ct)
    {
        var agents = await db.Set<Agent>()
            .AsNoTracking()
            .Include(agent => agent.Specializations)
            .ToListAsync(ct);

        var openCounts = await db.Set<Ticket>()
            .AsNoTracking()
            .Where(ticket => ticket.AssignedAgentId != null
                && ticket.Status != TicketStatus.Resolved
                && ticket.Status != TicketStatus.Closed)
            .GroupBy(ticket => ticket.AssignedAgentId)
            .Select(group => new { AgentId = group.Key, Count = group.Count() })
            .ToListAsync(ct);

        var counts = openCounts.ToDictionary(row => row.AgentId!.Value, row => row.Count);

        return agents
            .Select(agent => new AgentCandidate(
                agent.Id,
                agent.FullName,
                agent.IsActive,
                agent.MaxOpenTickets,
                counts.GetValueOrDefault(agent.Id),
                agent.Specializations.Select(specialization => specialization.CategoryId).ToHashSet()))
            .ToList();
    }
}
