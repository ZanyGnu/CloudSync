
namespace CloudSync.PicasaDownloader
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    
    class Program
    {
        static string UsernameAppSettingName = "username";
        static string DestinationPathAppSettingName = "destinationPath";

        static void Main(string[] args)
        {
            Settings settings = GetInputSettings();

            Downloader downloader = new Downloader(settings);
            downloader.DownloadMetadata();
            downloader.DownloadFiles();            
        }

        private static Settings GetInputSettings()
        {
            Settings settings = new Settings();

            settings.DestinationRootPath = ConfigurationManager.AppSettings[DestinationPathAppSettingName];
            settings.Username = ConfigurationManager.AppSettings[UsernameAppSettingName];

            if (String.IsNullOrWhiteSpace(settings.Username))
            {
                Console.Write("Enter username: ");
                settings.Username = Console.ReadLine();
            }

            Console.Write("Enter password: ");
            settings.Password = Utils.GetPassword();
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(settings.DestinationRootPath))
            {
                Console.Write("Enter destination path: ");
                settings.DestinationRootPath = Console.ReadLine();
            }

            return settings;
        }

    }
}
