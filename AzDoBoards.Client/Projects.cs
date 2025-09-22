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

    /// <summary>
    /// Gets projects that use a specific process template (optimized to filter during retrieval)
    /// </summary>
    /// <param name="processId">The process template ID to filter by</param>
    /// <returns>List of projects using the specified process template</returns>
    public async Task<List<ProjectInfo>> GetProjectInfoAsync(Guid processId)
    {
        var connection = await _connectionFactory.GetConnectionAsync();
        var projectClient = connection.GetClient<ProjectHttpClient>();

        var allProjects = await projectClient.GetProjects(); // Get all projects first (this is necessary as there's no direct API to filter by process)
        var filteredProjects = new List<ProjectInfo>();

        // Filter projects by checking each project's process template
        foreach (var project in allProjects)
        {
            try
            {
                // Get project details including capabilities to access process template information
                var projectDetail = await projectClient.GetProject(project.Id.ToString(), includeCapabilities: true);

                // Check if this project uses the specified process template
                if (projectDetail.Capabilities?.TryGetValue("processTemplate", out var processTemplate) == true &&
                    processTemplate.TryGetValue("templateTypeId", out var templateTypeId) &&
                    Guid.TryParse(templateTypeId, out var projectProcessId) &&
                    projectProcessId == processId)
                {
                    filteredProjects.Add(new ProjectInfo
                    {
                        Id = project.Id,
                        Name = project.Name,
                        Description = project.Description ?? string.Empty,
                        State = project.State.ToString(),
                        Visibility = project.Visibility.ToString()
                    });
                }
            }
            catch
            {
                // Skip projects that can't be accessed or don't have process template info
                continue;
            }
        }

        return filteredProjects;
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