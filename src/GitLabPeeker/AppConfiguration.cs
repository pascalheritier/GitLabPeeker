namespace GitLabPeeker;

internal class AppConfiguration
{
    public string PAT { get; set; } = null!;
    public string GitLabUrl { get; set; } = null!;
    public string GroupPeeking { get; set; } = null!;
    public int MinRefreshRateSeconds { get; set; }
}
