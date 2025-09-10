using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;

namespace GitLabPeeker;

class Program
{
    #region Members

    private static string AppSettingsFileName = "appsettings.json";
    private static string LogConfigFileName = "NLog.config";

    private static bool m_bMustExit;

    #endregion

    #region Main

    static void Main(string[] args)
    {
        try
        {
            IServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            do
            {
                PrintMenu();
                SelectChoice(serviceProvider);
            } while (!m_bMustExit);
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Log(NLog.LogLevel.Fatal, $"Critical app failure: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
    }

    private static void PrintMenu()
    {
        Console.WriteLine("---------------------GITLAB HELPER---------------------");
        Console.WriteLine("----------------------Menu---------------------");
        Console.WriteLine("1. Pipeline peeker");
        Console.WriteLine("2. Branch peeker");
        Console.WriteLine("0. Exit");
    }

    private static void SelectChoice(IServiceProvider serviceProvider)
    {
        string? menuChoice = Console.ReadLine();
        if (int.TryParse(menuChoice, out int menuChoiceInt))
        {
            switch (menuChoiceInt)
            {
                case 0:
                    m_bMustExit = true;
                    break;
                case 1:
                    var pipelinePeeker = serviceProvider.GetRequiredService<PipelinePeeker>();
                    pipelinePeeker.Run();
                    break;
                case 2:
                    var branchPeeker = serviceProvider.GetRequiredService<BranchPeeker>();
                    string? branchFilter;
                    do
                    {
                        Console.Write("Branch filter: ");
                        branchFilter = Console.ReadLine();
                        if (branchFilter is null)
                            Console.WriteLine("Branch filter should not be null.");
                    } while (branchFilter is null);
                    branchPeeker.Run(branchFilter);
                    break;
                default:
                    Console.WriteLine($"Entry {menuChoiceInt} is out of range");
                    break;
            }
        }
    }

    #endregion

    #region Services

    private static void ConfigureServices(IServiceCollection services)
    {
        IConfiguration config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory()) //From NuGet Package Microsoft.Extensions.Configuration.Json
        .AddJsonFile(AppSettingsFileName, optional: true, reloadOnChange: true)
        .Build();

        services.AddSingleton<AppConfiguration>(_X => GetAppConfiguration(config));
        services.AddLogging(loggingBuilder =>
        {
            // configure Logging with NLog
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            loggingBuilder.AddNLog(GetLogConfiguration());
        });
        services.AddTransient<PipelinePeeker>();
        services.AddTransient<BranchPeeker>();
    }

    private static LoggingConfiguration GetLogConfiguration()
    {
        Stream stream = typeof(Program).Assembly.GetManifestResourceStream("GitLabPeeker." + LogConfigFileName)!;
        string xml;
        using (var reader = new StreamReader(stream))
        {
            xml = reader.ReadToEnd();
        }
        return XmlLoggingConfiguration.CreateFromXmlString(xml);
    }

    private static AppConfiguration GetAppConfiguration(IConfiguration configuration)
    {
        AppConfiguration appConfiguration = new();
        configuration.Bind(appConfiguration);
        return appConfiguration;
    }

    #endregion
}