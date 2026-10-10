using SupportDesk.Infrastructure.Services;

namespace SupportDesk.UnitTests.Infrastructure.Services;

public class IdentityPasswordHasherTests
{
    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Verify_AcceptsThePasswordThatWasHashed()
    {
        var hash = _hasher.Hash("Local_Dev_AgentPassword1");

        Assert.NotEqual("Local_Dev_AgentPassword1", hash);
        Assert.True(_hasher.Verify(hash, "Local_Dev_AgentPassword1"));
    }

    [Fact]
    public void Verify_RejectsADifferentPassword()
    {
        var hash = _hasher.Hash("Local_Dev_AgentPassword1");

        Assert.False(_hasher.Verify(hash, "wrong-password"));
    }

    [Fact]
    public void Hash_UsesADifferentSaltEachTime()
    {
        var first = _hasher.Hash("Local_Dev_AgentPassword1");
        var second = _hasher.Hash("Local_Dev_AgentPassword1");

        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify(first, "Local_Dev_AgentPassword1"));
        Assert.True(_hasher.Verify(second, "Local_Dev_AgentPassword1"));
    }
}
