namespace SupportDesk.Application.Contracts.Auth;

/// <summary>Body of POST /api/auth/login.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Who is signed in. Enough to greet them; not a profile.</summary>
public sealed record CurrentAgentDto(int Id, string FullName, string Email);

/// <summary>Body of a successful login.</summary>
public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, CurrentAgentDto Agent);

/// <summary>The only message a failed sign-in ever returns.</summary>
public static class LoginFailure
{
    public const string Message = "The email or password is incorrect.";
}
