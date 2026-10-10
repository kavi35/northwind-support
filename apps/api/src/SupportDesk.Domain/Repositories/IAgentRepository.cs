using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Repositories;

/// <summary>
/// Loads <see cref="Agent"/> aggregates.
/// </summary>
public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>The agent with this email, compared without regard to case, or null.</summary>
    Task<Agent?> FindByEmailAsync(string email, CancellationToken ct);

    /// <summary>Every agent, with how many open tickets they currently hold.</summary>
    Task<IReadOnlyList<AgentCandidate>> ListCandidatesAsync(CancellationToken ct);
}
