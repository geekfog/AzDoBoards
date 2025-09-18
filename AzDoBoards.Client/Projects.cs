using Microsoft.TeamFoundation.Core.WebApi;

namespace AzDoBoards.Client;

public class Projects(ConnectionFactory connectionFactory) : Base(connectionFactory)
{
    public async Task<List<string>> GetProjectsAsync()
    {
        var connection = await _connectionFactory.GetConnectionAsync();
        var projectClient = connection.GetClient<ProjectHttpClient>();
        var projects = await projectClient.GetProjects();
        return [.. projects.Select(p => p.Name)];
    }

    public async Task<List<ProjectInfo>> GetProjectInfoAsync()
    {
        var connection = await _connectionFactory.GetConnectionAsync();
        var projectClient = connection.GetClient<ProjectHttpClient>();
        var projects = await projectClient.GetProjects();
        return [.. projects.Select(p => new ProjectInfo
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description ?? string.Empty,
            State = p.State.ToString(),
            Visibility = p.Visibility.ToString()
        })];
    }
}

/// <summary>
/// Information about a project in Azure DevOps
/// </summary>
public class ProjectInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
}