
namespace PicasaDownloaderApp
{
    using CloudSync.PicasaDownloader;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public partial class DownloaderForm : Form
    {
        public DownloaderForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            textBoxDestinationPath.Text = folderBrowserDialog1.SelectedPath;
        }

        private void textBoxUserName_TextChanged(object sender, EventArgs e)
        {
            EnableOrDisableDownloadButton();
        }

        private void EnableOrDisableDownloadButton()
        {
            if (!String.IsNullOrWhiteSpace(textBoxUserName.Text)
                && !String.IsNullOrWhiteSpace(textBoxDestinationPath.Text)
                && !String.IsNullOrWhiteSpace(maskedTextBoxPassword.Text))
            {
                buttonDownload.Enabled = true;
            }
            else
            {
                buttonDownload.Enabled = false;
            }
        }

        private void maskedTextBoxPassword_TextChanged(object sender, EventArgs e)
        {
            EnableOrDisableDownloadButton();
        }

        private void textBoxDestinationPath_TextChanged(object sender, EventArgs e)
        {
            EnableOrDisableDownloadButton();
        }

        IProgress<int> reportIncrement = null;
        IProgress<int> reportMaximum = null;

        private async void buttonDownload_Click(object sender, EventArgs e)
        {
            Settings settings = new Settings();
            settings.Username = textBoxUserName.Text;
            settings.Password = maskedTextBoxPassword.Text;
            settings.DestinationRootPath = textBoxDestinationPath.Text;

            reportIncrement = new Progress<int>(value => 
                {
                    progressBar.Increment(value);
                    int percent = (int)(((double)progressBar.Value / (double)progressBar.Maximum) * 100);

                    labelProgress.Text = String.Format("[{0} of {1}] {2} % completed", progressBar.Value.ToString("D4"), progressBar.Maximum.ToString("D4"), percent);
                });
            reportMaximum = new Progress<int>(value => progressBar.Maximum = value);
            progressBar.Minimum = 0;
            progressBar.Value = 0;
            
            buttonDownload.Enabled = false;

            Downloader downloader = null;

            toolStripStatusLabel.Text = "Getting information from picasa service";

            await Task.Factory.StartNew(
                () => downloader = this.DownloadMetadata(settings),
                TaskCreationOptions.LongRunning);

            progressBar.Maximum = (int)downloader.TotalImageCount;
            //reportMaximum.Report((int)downloader.TotalImageCount);

            await Task.Factory.StartNew(
                () => this.DownloadFiles(downloader),
                TaskCreationOptions.LongRunning);

            buttonDownload.Enabled = true;
            toolStripStatusLabel.Text = "Done";
            pictureBox.ImageLocation = "img\\picasa-icon.png";
        }

        private Downloader DownloadMetadata(Settings settings)
        {
            Downloader downloader = new Downloader(settings);

            downloader.OnBeforeDownloadingEvent += downloader_OnBeforeDownloadingEvent;
            downloader.OnAfterDownloadCompleteEvent += downloader_OnAfterDownloadCompleteEvent;
            downloader.DownloadMetadata();
            return downloader;
        }

        private void DownloadFiles(Downloader downloader)
        {            
            downloader.DownloadFiles();
        }

        void downloader_OnAfterDownloadCompleteEvent(string photoName, string localPath)
        {
            reportIncrement.Report(1);
            toolStripStatusLabel.Text = "";
            pictureBox.Invoke((MethodInvoker)delegate 
            {
                pictureBox.ImageLocation = localPath; 
            }
            );
        }

        void downloader_OnBeforeDownloadingEvent(string photoName, string url)
        {
            UpdateStatus("Downloading " + photoName + ".");
        }

        private void UpdateStatus(string text)
        {
            toolStripStatusLabel.Text = text;
        }

    }

    public static class Extensions
    {
        private delegate void SetPropertyThreadSafeDelegate<TResult>(Control @this, Expression<Func<TResult>> property, TResult value);

        public static void SetPropertyThreadSafe<TResult>(this Control @this, Expression<Func<TResult>> property, TResult value)
        {
            var propertyInfo = (property.Body as MemberExpression).Member as PropertyInfo;

            if (propertyInfo == null ||
                !@this.GetType().IsSubclassOf(propertyInfo.ReflectedType) ||
                @this.GetType().GetProperty(propertyInfo.Name, propertyInfo.PropertyType) == null)
            {
                throw new ArgumentException("The lambda expression 'property' must reference a valid property on this Control.");
            }

            if (@this.InvokeRequired)
            {
                @this.Invoke(new SetPropertyThreadSafeDelegate<TResult>(SetPropertyThreadSafe), new object[] { @this, property, value });
            }
            else
            {
                @this.GetType().InvokeMember(propertyInfo.Name, BindingFlags.SetProperty, null, @this, new object[] { value });
            }
        }
    }
}
