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
}
