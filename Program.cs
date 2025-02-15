using Microsoft.Extensions.Configuration;

namespace SimpleYoutubeDownloader
{

    internal static class Program
    {

        private static readonly string CookiesFilePath = "cookies.json";
        private static readonly string ConfigFilePath = "appsettings.json";
        public static bool isPrivate;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile(ConfigFilePath, optional: false, reloadOnChange: true);
            IConfiguration config = builder.Build();

            isPrivate = Boolean.Parse(config["isPrivate"]);

            Application.ApplicationExit += OnApplicationExit;
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(isPrivate));
        }
        private static void OnApplicationExit(object sender, EventArgs e)
        {
            try
            {
                if (isPrivate && File.Exists(CookiesFilePath))
                {
                    File.Delete(CookiesFilePath);
                    Console.WriteLine("cookies.json deleted.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error");
            }
        }

    }
}

