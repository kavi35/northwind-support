using Microsoft.Extensions.Configuration;

namespace SupportDesk.Presentation.Auth;

/// <summary>JWT settings. The signing key is configuration, never a literal in code.</summary>
public sealed record JwtOptions(string Issuer, string Audience, string SigningKey, int LifetimeMinutes)
{
    public static JwtOptions Bind(IConfiguration configuration)
    {
        var section = configuration.GetSection("Jwt");
        var signingKey = section["SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is missing or shorter than 32 characters. Set Jwt__SigningKey. See .env.example.");
        }

        var lifetimeText = section["LifetimeMinutes"];
        if (!int.TryParse(lifetimeText, out var lifetimeMinutes) || lifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:LifetimeMinutes must be a positive number of minutes.");
        }

        var issuer = section["Issuer"];
        var audience = section["Audience"];

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
        }

        return new JwtOptions(issuer, audience, signingKey, lifetimeMinutes);
    }
}
