using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.GetCurrentAgent;

/// <summary>Returns the agent named by the current token, or rejects the call.</summary>
public sealed class GetCurrentAgentQueryHandler(ICurrentAgent current, IAgentRepository agents)
{
    /// <exception cref="UnauthorizedException">The token does not name an active agent.</exception>
    public async Task<CurrentAgentDto> HandleAsync(CancellationToken ct)
    {
        var agent = await agents.GetByIdAsync(current.Id, ct);

        if (agent is null || !agent.IsActive)
        {
            throw new UnauthorizedException("Sign in is required.");
        }

        return new CurrentAgentDto(agent.Id, agent.FullName, agent.Email);
    }
}
