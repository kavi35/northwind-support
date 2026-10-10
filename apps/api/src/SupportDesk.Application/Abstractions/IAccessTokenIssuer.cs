namespace SupportDesk.Application.Abstractions;

/// <summary>A signed access token and the instant it stops being accepted.</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

/// <summary>Creates the credential a signed-in agent sends on later requests.</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(int agentId, string fullName, string email);
}
