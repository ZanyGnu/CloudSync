
namespace CloudSync.PicasaDownloader
{
    using Google.GData.Photos;
    using Google.Picasa;

    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Net;
    
    class Program
    {
        static string ApplicationName = "cloudsync-picasadownloader-v1";
        static string UsernameAppSettingName = "username";
        static string DestinationPathEntry = "destinationPath";

        static void Main(string[] args)
        {
            string destinationRootPath = ConfigurationManager.AppSettings[DestinationPathEntry];
            string username = ConfigurationManager.AppSettings[UsernameAppSettingName];
            destinationRootPath = Path.Combine(destinationRootPath, username);

            Console.Write("Enter password: ");
            string password = Utils.GetPassword();
            Console.WriteLine();

            PicasaService service = new PicasaService(ApplicationName);
            service.setUserCredentials(username, password);

            AlbumQuery query = new AlbumQuery(PicasaQuery.CreatePicasaUri(username));
            query.ExtraParameters += "imgmax=d"; // passing this param allows us to get the original image size.

            PicasaFeed feed = service.Query(query);

            foreach (PicasaEntry entry in feed.Entries)
            {                
                Album album = GetAlbum(entry);

                GetPhotosFromAlbum(service, album, destinationRootPath);
            }
        }

        private static Album GetAlbum(PicasaEntry entry)
        {
            Album album = new Album();

            album.Title = entry.Title.Text;
            foreach (var extemsionItem in entry.ExtensionElements)
            {
                switch (extemsionItem.XmlName.ToLower())
                {
                    case "id":
                        album.Id = ((GPhotoId)extemsionItem).Value;
                        break;
                    case "numphotos":
                        album.NumPhotos = (uint)((GPhotoNumPhotos)extemsionItem).IntegerValue;
                        break;
                }                
            }

            return album;
        }

        private static void GetPhotosFromAlbum(
            PicasaService service,
            Album album, 
            string destinationRootPath)
        {
            int pageSize = 500;
            long pageCount = album.NumPhotos/pageSize;
            int currentPage = 0;
            int num = 0;

            Console.WriteLine("Album: {0} ({1})", album.Title, album.NumPhotos);

            while (currentPage <= pageCount)
            {
                PhotoQuery query = new PhotoQuery(PicasaQuery.CreatePicasaUri(service.Credentials.Username, album.Id));
                query.NumberToRetrieve = pageSize;
                query.StartIndex = currentPage * pageSize;
                query.ExtraParameters += "imgmax=d";

                PicasaFeed feed = service.Query(query);

                string destinationFolder = Path.Combine(destinationRootPath, Utils.ScrubStringForFileSystem(album.Title));
                
                foreach (PicasaEntry entry in feed.Entries)
                {
                    Console.Write("\t [{0}/{1}]", num++.ToString("D4"), album.NumPhotos.ToString("D4"));
                    DownloadPhotoDetails(entry, service, destinationFolder);
                }

                currentPage++;
            }
        }

        private static void DownloadPhotoDetails(PicasaEntry entry, PicasaService service, string destinationFolder)
        {
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            string destinationFilePath = Path.Combine(destinationFolder, Utils.ScrubStringForFileSystem(entry.Title.Text));

            if (File.Exists(destinationFilePath))
            {
                Console.WriteLine("\tSkipping {0}", entry.Title.Text);
            }
            else
            {
                Console.Write("\tDownloading {0}", entry.Title.Text);

                WebClient webClient = new WebClient();

                string url = entry.Media.Contents.First().Url;
                webClient.DownloadFile(url, destinationFilePath);
                
                Console.WriteLine(" [Done]");
            }
        }
    }
}
