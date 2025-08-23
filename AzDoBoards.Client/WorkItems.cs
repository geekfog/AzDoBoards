using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;

namespace AzDoBoards.Client;

public class WorkItems
{
    private WorkItemTrackingHttpClient _workClient;

    public WorkItems()
    {
        const string organization = "aeriden";
        var credentials = new VssAadCredential(); // Uses logged-in user's token (via Entra ID)
        var connection = new VssConnection(new Uri($"https://dev.azure.com/{organization}"), credentials);
        _workClient = connection.GetClient<WorkItemTrackingHttpClient>();
    }

}
