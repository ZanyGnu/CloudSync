
namespace PicasaDownloaderApp
{
    using CloudSync.PicasaDownloader;
    using Google.GData.Client;
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
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxDestinationPath.Text = folderBrowserDialog1.SelectedPath;
            }
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

        private async void buttonDownload_Click(object sender, EventArgs e)
        {
            Downloader downloader = null;
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


            // Initialize UI to start processing data.
            progressBar.Minimum = 0;
            progressBar.Value = 0;

            checkBoxShowErrorDetails.Visible = false;
            textBoxErrorMessage.Visible = false;
            textBoxErrorMessage.Text = string.Empty;
            checkBoxShowErrorDetails.Checked = false;
            
            // Prevent invoking another download
            buttonDownload.Enabled = false;

            toolStripStatusLabel.Text = "Getting information from picasa service";

            try
            {
                await Task.Factory.StartNew(
                    () => downloader = this.DownloadMetadata(settings),
                    TaskCreationOptions.LongRunning);

                progressBar.Maximum = (int)downloader.TotalImageCount;

                await Task.Factory.StartNew(
                    () => this.DownloadFiles(downloader),
                    TaskCreationOptions.LongRunning);

                toolStripStatusLabel.Text = "Done";

            }
            catch (InvalidCredentialsException invalidCredentials)
            {
                toolStripStatusLabel.Text = "Could not login. Incorrect username or password?";
                checkBoxShowErrorDetails.Visible = true;
                textBoxErrorMessage.Text = invalidCredentials.ToString();
            }

            catch (GDataRequestException requestException)
            {
                toolStripStatusLabel.Text = "Could not connect. Check internet connection?";
                checkBoxShowErrorDetails.Visible = true;
                textBoxErrorMessage.Text = requestException.ToString();
            }
            catch (Exception exception)
            {
                toolStripStatusLabel.Text = "Done with errors. (" + exception.ToString().Substring(0, 50) + "...";
                textBoxErrorMessage.Text = exception.ToString();
                checkBoxShowErrorDetails.Visible = true;
            }
            finally
            {
                buttonDownload.Enabled = true;
                pictureBox.ImageLocation = "img\\picasa-icon.png";
            }
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
            pictureBox.Invoke(
                (MethodInvoker) delegate 
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

        private void checkBoxShowErrorDetails_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxShowErrorDetails.Checked)
            {
                textBoxErrorMessage.Visible = true;
                textBoxErrorMessage.BringToFront();
            }
            else
            {
                textBoxErrorMessage.Visible = false;
            }
        }

    }
}
