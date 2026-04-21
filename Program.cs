using Microsoft.Extensions.Configuration;

namespace SimpleYoutubeDownloader;

internal static class Program
{
    private static readonly string ConfigFilePath = "appsettings.json";
    public static bool isPrivate;

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        try
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        }
        catch
        {
            // ignorado
        }

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(ConfigFilePath, false, true);
        IConfiguration config = builder.Build();

        isPrivate = bool.Parse(config["isPrivate"]);

        Application.ApplicationExit += OnApplicationExit;
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(isPrivate));
    }

    private static void OnApplicationExit(object sender, EventArgs e)
    {
        try
        {
            if (isPrivate)
            {
                YouTubeSessionStore.ClearAllPersistedAndMemory();
                Console.WriteLine("Sessão YouTube (memória + ficheiros) removida (modo privado).");
            }
        }
        catch
        {
            Console.WriteLine("Error");
        }
    }
}