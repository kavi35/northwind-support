namespace SupportDesk.Application.Abstractions;

/// <summary>The agent authenticated for the current request.</summary>
public interface ICurrentAgent
{
    int Id { get; }

    string FullName { get; }
}
