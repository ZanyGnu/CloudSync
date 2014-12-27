
namespace CloudSync.PicasaDownloader
{
    using Google.GData.Photos;
    using Google.Picasa;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    public class Downloader
    {
        static string ApplicationName = "cloudsync-picasadownloader-v1";

        internal PicasaService Service { get; set; }
        internal string Username { get; set; }
        internal string DestinationRootPath { get; set; }

        public List<Album> Albums { get; set; }

        /// <summary>
        /// The count of all images for this account.
        /// </summary>
        public uint TotalImageCount { get; set; }

        public delegate void OnBeforeDownloading(string photoName, string url);
        public delegate void OnAfterDownloadComplete(string photoName, string localPath);

        public event OnBeforeDownloading OnBeforeDownloadingEvent;

        public event OnAfterDownloadComplete OnAfterDownloadCompleteEvent;        

        public Downloader(Settings settings)
        {
            this.DestinationRootPath = settings.DestinationRootPath;
            Username = settings.Username;
            string password = settings.Password;

            this.DestinationRootPath = Path.Combine(settings.DestinationRootPath, settings.Username);

            Service = new PicasaService(ApplicationName);
            Service.setUserCredentials(Username, password);

            this.Albums = new List<Album>();
        }

        public void DownloadMetadata()
        {
            AlbumQuery query = new AlbumQuery(PicasaQuery.CreatePicasaUri(Username));
            query.ExtraParameters += "imgmax=d"; // passing this param allows us to get the original image size.

            PicasaFeed feed = Service.Query(query);

            foreach (PicasaEntry entry in feed.Entries)
            {
                Album album = GetAlbum(entry);
                this.Albums.Add(album);

                this.TotalImageCount += album.NumPhotos;
            }
        }

        public void DownloadFiles()
        {
            foreach (Album album in this.Albums)
            {
                GetPhotosFromAlbum(album);
            }
        }

        private Album GetAlbum(PicasaEntry entry)
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

        private void GetPhotosFromAlbum(Album album)
        {
            int pageSize = 500;
            long pageCount = album.NumPhotos / pageSize;
            int currentPage = 0;
            int num = 0;

            Console.WriteLine("Album: {0} ({1})", album.Title, album.NumPhotos);

            while (currentPage <= pageCount)
            {
                PhotoQuery query = new PhotoQuery(PicasaQuery.CreatePicasaUri(this.Service.Credentials.Username, album.Id));
                query.NumberToRetrieve = pageSize;
                query.StartIndex = currentPage * pageSize;
                query.ExtraParameters += "imgmax=d";

                PicasaFeed feed = this.Service.Query(query);

                string destinationFolder = Path.Combine(this.DestinationRootPath, Utils.ScrubStringForFileSystem(album.Title));

                foreach (PicasaEntry entry in feed.Entries)
                {
                    Console.Write("\t [{0}/{1}]", num++.ToString("D4"), album.NumPhotos.ToString("D4"));
                    DownloadPhotoDetails(entry, this.Service, destinationFolder);
                }

                currentPage++;
            }
        }

        private void DownloadPhotoDetails(PicasaEntry entry, PicasaService service, string destinationFolder)
        {
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            string destinationFilePath = Path.Combine(destinationFolder, Utils.ScrubStringForFileSystem(entry.Title.Text));
            string url = entry.Media.Contents.First().Url;

            this.OnBeforeDownloadingEvent.Invoke(entry.Title.Text, url);

            if (File.Exists(destinationFilePath))
            {
                Console.WriteLine("\tSkipping {0}", entry.Title.Text);
            }
            else
            {
                Console.Write("\tDownloading {0}", entry.Title.Text);

                WebClient webClient = new WebClient();

                webClient.DownloadFile(url, destinationFilePath);
                Console.WriteLine(" [Done]");
            }

            OnAfterDownloadCompleteEvent.Invoke(entry.Title.Text, destinationFilePath);
        }
    }
}
