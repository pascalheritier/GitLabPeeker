using GitLabApiClient;
using GitLabApiClient.Models.Branches.Responses;
using GitLabApiClient.Models.Projects.Responses;
using Microsoft.Extensions.Logging;

namespace GitLabPeeker;

internal class BranchPeeker
{
    #region Members

    private ILogger _logger;
    private AppConfiguration _appConfiguration;

    #endregion

    #region Constructor

    public BranchPeeker(AppConfiguration appConfiguration, ILoggerFactory loggerFactory)
    {
        _appConfiguration = appConfiguration;
        _logger = loggerFactory.CreateLogger<BranchPeeker>();
    }

    #endregion

    #region Peeking behaviour

    public void Run(string filter)
    {
        GetBranchesMatchingFilter(filter).Wait();
    }

    private async Task GetBranchesMatchingFilter(string filter)
    {
        var client = new GitLabClient(_appConfiguration.GitLabUrl, _appConfiguration.PAT);

        // Get all accessible projects (may need paging)
        var projects = await client.Projects.GetAsync();

        foreach (Project project in projects)
        {
            // Get branches for this project
            var branches = await client.Branches.GetAsync(project.Id, o => { });

            // Filter branches by name
            var matchingBranches = branches.Where(b => b.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

            if (matchingBranches.Any())
            {
                Console.WriteLine($"Project: {project.PathWithNamespace}");
                foreach (Branch branch in matchingBranches)
                {
                    Console.WriteLine($"  - {branch.Name}");
                }
                Console.WriteLine();
            }
        }
    }

    #endregion
}