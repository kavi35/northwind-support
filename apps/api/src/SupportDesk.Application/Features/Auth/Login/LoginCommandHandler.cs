using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.Login;

/// <summary>
/// Checks an agent's email and password and issues a token. Unknown email, wrong password
/// and an inactive agent all fail the same way.
/// </summary>
public sealed class LoginCommandHandler(
    IAgentRepository agents,
    IPasswordHasher passwords,
    IAccessTokenIssuer tokens)
{
    private readonly string _dummyHash = passwords.Hash("not-a-password");

    /// <exception cref="UnauthorizedException">The credentials are not accepted.</exception>
    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken ct)
    {
        var agent = await agents.FindByEmailAsync(request.Email.Trim(), ct);
        var hash = agent?.PasswordHash ?? _dummyHash;
        var passwordMatches = passwords.Verify(hash, request.Password);

        if (agent is null || agent.PasswordHash is null || !agent.IsActive || !passwordMatches)
        {
            throw new UnauthorizedException(LoginFailure.Message);
        }

        var access = tokens.Issue(agent.Id, agent.FullName, agent.Email);

        return new LoginResponse(
            access.Token,
            access.ExpiresAtUtc,
            new CurrentAgentDto(agent.Id, agent.FullName, agent.Email));
    }
}
