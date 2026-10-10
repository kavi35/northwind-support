using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application.Abstractions;

namespace SupportDesk.Presentation.Auth;

/// <summary>Signs a JWT with HMAC-SHA256. Lifetime and key come from <see cref="JwtOptions"/>.</summary>
public sealed class JwtAccessTokenIssuer(JwtOptions options, IClock clock) : IAccessTokenIssuer
{
    public AccessToken Issue(int agentId, string fullName, string email)
    {
        var expiresAtUtc = clock.UtcNow.AddMinutes(options.LifetimeMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, agentId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, agentId.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email)
            ],
            notBefore: clock.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
