namespace SupportDesk.Application.Abstractions;

/// <summary>
/// Turns a password into a salted hash and checks a password against one. The implementation
/// is a vetted algorithm; this interface is the seam the login and the seeder share.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}
