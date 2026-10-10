namespace SupportDesk.Application.Exceptions;

/// <summary>Thrown when a caller is not allowed to continue. Surfaces as HTTP 401.</summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
