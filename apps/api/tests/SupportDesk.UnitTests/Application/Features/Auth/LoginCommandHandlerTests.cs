using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Auth.Login;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.UnitTests.Application.Features.Auth;

public class LoginCommandHandlerTests
{
    private const string Password = "correct-password";

    [Fact]
    public async Task HandleAsync_WhenThePasswordIsWrong_Is401AndIssuesNoToken()
    {
        var context = Context(agent: ActiveAgent(), passwordMatches: false);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => context.Handler.HandleAsync(new LoginRequest(context.Agent.Email, "wrong"), CancellationToken.None));

        Assert.Equal(LoginFailure.Message, exception.Message);
        context.Tokens.Verify(
            token => token.Issue(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenTheEmailIsUnknown_UsesTheSameMessageAndIssuesNoToken()
    {
        var context = Context(agent: null, passwordMatches: false);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => context.Handler.HandleAsync(new LoginRequest("missing@example.com", Password), CancellationToken.None));

        Assert.Equal(LoginFailure.Message, exception.Message);
        context.Tokens.Verify(
            token => token.Issue(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenTheAgentIsInactive_UsesTheSameMessage()
    {
        var agent = ActiveAgent();
        agent.Deactivate();
        var context = Context(agent, passwordMatches: true);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => context.Handler.HandleAsync(new LoginRequest(agent.Email, Password), CancellationToken.None));

        Assert.Equal(LoginFailure.Message, exception.Message);
        context.Tokens.Verify(
            token => token.Issue(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenTheCredentialsMatch_ReturnsATokenAndTheAgent()
    {
        var context = Context(ActiveAgent(), passwordMatches: true);
        var expires = new DateTime(2026, 10, 11, 16, 0, 0, DateTimeKind.Utc);
        context.Tokens
            .Setup(token => token.Issue(context.Agent.Id, context.Agent.FullName, context.Agent.Email))
            .Returns(new AccessToken("signed-token", expires));

        var response = await context.Handler.HandleAsync(
            new LoginRequest(context.Agent.Email, Password),
            CancellationToken.None);

        Assert.Equal("signed-token", response.Token);
        Assert.Equal(expires, response.ExpiresAtUtc);
        Assert.Equal(context.Agent.FullName, response.Agent.FullName);
        Assert.Equal(context.Agent.Email, response.Agent.Email);
    }

    private static Agent ActiveAgent()
    {
        var agent = new Agent("Alex Turner", "alex.turner@northwind-support.example", 10, DateTime.UtcNow);
        agent.SetPasswordHash("stored-hash");
        return agent;
    }

    private static LoginContext Context(Agent? agent, bool passwordMatches)
    {
        var agents = new Mock<IAgentRepository>();
        agents.Setup(repository => repository.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        var passwords = new Mock<IPasswordHasher>();
        passwords.Setup(hasher => hasher.Hash(It.IsAny<string>())).Returns("dummy-hash");
        passwords.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(passwordMatches);

        var tokens = new Mock<IAccessTokenIssuer>();
        var handler = new LoginCommandHandler(agents.Object, passwords.Object, tokens.Object);

        return new LoginContext(handler, tokens, agent ?? ActiveAgent());
    }

    private sealed class LoginContext(LoginCommandHandler handler, Mock<IAccessTokenIssuer> tokens, Agent agent)
    {
        public LoginCommandHandler Handler { get; } = handler;

        public Mock<IAccessTokenIssuer> Tokens { get; } = tokens;

        public Agent Agent { get; } = agent;
    }
}
