using GitLabApiClient;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Pipelines;
using GitLabApiClient.Models.Pipelines.Responses;
using GitLabApiClient.Models.Projects.Responses;
using Microsoft.Extensions.Logging;
using System.Reflection.Emit;

namespace GitLabPeeker;

internal class Peeker
{
    #region Members

    private ILogger _logger;
    private AppConfiguration _appConfiguration;

    #endregion

    #region Constructor

    public Peeker(AppConfiguration appConfiguration, ILoggerFactory loggerFactory)
    {
        _appConfiguration = appConfiguration;
        _logger = loggerFactory.CreateLogger<Peeker>();
    }

    #endregion

    #region Peeking behaviour

    public void Run()
    {
        ShowRunningPipelines().Wait();
    }

    private async Task ShowRunningPipelines()
    {
        bool stopAsked = false;
        while (!stopAsked)
        {
            try
            {
                var client = new GitLabClient(_appConfiguration.GitLabUrl, _appConfiguration.PAT);
                IList<Group> peekingGroups = await client.Groups.SearchAsync(_appConfiguration.GroupPeeking);
                List<Project> peekingProjects = new();
                foreach (Group group in peekingGroups)
                    peekingProjects.AddRange(await client.Groups.GetProjectsAsync(group.Id));
                Dictionary<Project, IEnumerable<Pipeline>> runningProjectPipelines = new();

                while (!stopAsked)
                {
                    DateTime start = DateTime.UtcNow;
                    runningProjectPipelines.Clear();
                    string logMessage = string.Empty;

                    // get pipelines info
                    foreach (Project project in peekingProjects)
                    {
                        IList<Pipeline> runningPipelines = await client.Pipelines.GetAsync(project, (option) => option.Status = PipelineStatus.Running);
                        if (runningPipelines.Count > 0)
                            runningProjectPipelines[project] = runningPipelines;
                    }

                    // print
                    logMessage += $"Total running pipelines (peeking group '{_appConfiguration.GroupPeeking}'): {runningProjectPipelines.Values.SelectMany(pipeline => pipeline).Count()}";
                    if (runningProjectPipelines.Count > 0)
                    {
                        logMessage += " | Projects: ";
                        logMessage += string.Join(", ", runningProjectPipelines.Keys.Select(project => $"{project.Name} ({runningProjectPipelines[project].Count()})"));
                    }
                    _logger.LogInformation(logMessage);
                    
                    // wait refresh rate
                    while (DateTime.UtcNow.AddSeconds(_appConfiguration.MinRefreshRateSeconds) < start)
                        Thread.Sleep(10);
                }
            }
            catch (HttpRequestException e)
            {
                _logger.LogError($"Bad connection, error message: {e.Message}");
            }
        }
    }

    #endregion
}