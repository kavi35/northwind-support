using System.Security.Claims;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Exceptions;

namespace SupportDesk.Presentation.Auth;

/// <summary>Reads the agent from the validated JWT on the current HTTP request.</summary>
public sealed class HttpCurrentAgent(IHttpContextAccessor accessor) : ICurrentAgent
{
    public int Id
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(value, out var id))
            {
                throw new UnauthorizedException("Sign in is required.");
            }

            return id;
        }
    }

    public string FullName =>
        User.FindFirstValue(ClaimTypes.Name)
        ?? throw new UnauthorizedException("Sign in is required.");

    private ClaimsPrincipal User =>
        accessor.HttpContext?.User ?? throw new UnauthorizedException("Sign in is required.");
}
