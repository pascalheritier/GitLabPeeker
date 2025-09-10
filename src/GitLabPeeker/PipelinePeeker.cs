using GitLabApiClient;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Pipelines;
using GitLabApiClient.Models.Pipelines.Responses;
using GitLabApiClient.Models.Projects.Responses;
using Microsoft.Extensions.Logging;

namespace GitLabPeeker;

internal class PipelinePeeker
{
    #region Members

    private ILogger _logger;
    private AppConfiguration _appConfiguration;

    #endregion

    #region Constructor

    public PipelinePeeker(AppConfiguration appConfiguration, ILoggerFactory loggerFactory)
    {
        _appConfiguration = appConfiguration;
        _logger = loggerFactory.CreateLogger<PipelinePeeker>();
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
                Dictionary<PipelineStatus, Dictionary<Project, IEnumerable<Pipeline>>> projectPipelines = new();

                while (!stopAsked)
                {
                    DateTime start = DateTime.UtcNow;
                    foreach (Dictionary<Project, IEnumerable<Pipeline>> projectPipelinesValue in projectPipelines.Values)
                    {
                        projectPipelinesValue.Clear();
                    }

                    // get pipelines info
                    foreach (Project project in peekingProjects)
                    {
                        foreach (PipelineStatus pipelineStatus in _appConfiguration.StatusToPeek)
                        {
                            IList<Pipeline> pipelines = await client.Pipelines.GetAsync(project, (option) => option.Status = pipelineStatus);
                            if (pipelines.Count > 0)
                            {
                                if (!projectPipelines.ContainsKey(pipelineStatus))
                                    projectPipelines[pipelineStatus] = new Dictionary<Project, IEnumerable<Pipeline>>();
                                projectPipelines[pipelineStatus][project] = pipelines;

                            }
                        }
                    }

                    // print
                    foreach (PipelineStatus pipelineStatus in _appConfiguration.StatusToPeek)
                    {
                        if (!projectPipelines.TryGetValue(pipelineStatus, out Dictionary<Project, IEnumerable<Pipeline>>? projectPipeline))
                        {
                            _logger.LogInformation($"No {pipelineStatus} pipeline (peeking group '{_appConfiguration.GroupPeeking}')");
                            continue;
                        }

                        string logMessage = $"Total {pipelineStatus} pipelines (peeking group '{_appConfiguration.GroupPeeking}'): {projectPipeline.Values.SelectMany(pipeline => pipeline).Count()}";
                        if (projectPipeline.Count > 0)
                        {
                            logMessage += " | Projects: ";
                            logMessage += string.Join(", ", projectPipeline.Keys.Select(project => $"{project.Name} ({projectPipeline[project].Count()})"));
                        }

                        _logger.LogInformation(logMessage);
                    }

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