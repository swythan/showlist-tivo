using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;

namespace TivoAhoy.Phone.ViewModels
{
    public class ShowDetailsPageViewModel : Screen
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ISterlingInstance sterlingInstance;
        private readonly SettingsPageViewModel settingsModel;

        private ShowDetails showDetails;
        private bool isOperationInProgress;

        public ShowDetailsPageViewModel(
            IEventAggregator eventAggregator,
            ISterlingInstance sterlingInstance,
            SettingsPageViewModel settingsModel)
        {
            this.eventAggregator = eventAggregator;
            this.sterlingInstance = sterlingInstance;
            this.settingsModel = settingsModel;
        }

        public bool IsOperationInProgress
        {
            get { return this.isOperationInProgress; }
        }

        private void OnOperationStarted()
        {
            this.isOperationInProgress = true;
            NotifyOfPropertyChange(() => this.IsOperationInProgress);
            NotifyOfPropertyChange(() => this.CanPlayShow);
        }

        private void OnOperationFinished()
        {
            this.isOperationInProgress = false;
            NotifyOfPropertyChange(() => this.IsOperationInProgress);
            NotifyOfPropertyChange(() => this.CanPlayShow);
        }

        public string ShowContentID { get; set; }
        public string ShowRecordingID { get; set; }
        public string ShowOfferID { get; set; }

        public bool HasEpisodeNumbers
        {
            get
            {
                return
                    this.Show != null &&
                    this.Show.EpisodeNumber != null &&
                    this.Show.SeasonNumber != null;
            }
        }

        public bool HasOriginalAirDate
        {
            get
            {
                return
                    this.Show != null &&
                    this.Show.OriginalAirDate != default(DateTime);
            }
        }

        public Uri MainImage
        {
            get
            {
                if (this.Show == null ||
                    this.Show.Images == null)
                {
                    return null;
                }

                var widthCutoff = 400;

                var imagesWithWidth = this.Show.Images.Where(x => x.Width != null);
                var largeImages = imagesWithWidth.Where(x => x.Width >= widthCutoff).OrderBy(x => x.Width);
                var smallImages = imagesWithWidth.Where(x => x.Width < widthCutoff).OrderByDescending(x => x.Width);

                var bestImage = largeImages.Concat(smallImages).Concat(this.Show.Images.Except(imagesWithWidth)).FirstOrDefault();

                if (bestImage == null)
                {
                    return null;
                }

                return bestImage.ImageUrl;
            }
        }

        public ShowDetails Show
        {
            get { return this.showDetails; }
            set
            {
                this.showDetails = value;

                var json = value.JsonText;
                Debug.WriteLine("Show details fetched:");

                foreach (var line in json.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    Debug.WriteLine(line);
                }

                Debug.WriteLine(string.Empty);

                NotifyOfPropertyChange(() => this.Show);
                NotifyOfPropertyChange(() => this.MainImage);
                NotifyOfPropertyChange(() => this.HasEpisodeNumbers);
                NotifyOfPropertyChange(() => this.HasOriginalAirDate);
                NotifyOfPropertyChange(() => this.CanPlayShow);
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            FetchShowDetails();
        }

        private async void FetchShowDetails()
        {
            var connection = new TivoConnection(sterlingInstance.Database);

            OnOperationStarted();

            try
            {
                await connection.ConnectAway(this.settingsModel.Username, this.settingsModel.Password);

                this.Show = await connection.GetShowContentDetails(this.ShowContentID);
            }
            catch (Exception ex)
            { 
                MessageBox.Show(string.Format("Failed to retrieve details:\n{0}", ex.Message));
            }
            finally
            { 
                connection.Dispose();
                OnOperationFinished();
            }
        }

        public async void PlayShow()
        {
            var connection = new TivoConnection(sterlingInstance.Database);

            OnOperationStarted();

            try
            {
                await connection.ConnectAway(this.settingsModel.Username, this.settingsModel.Password);
                await connection.PlayShow(this.ShowRecordingID);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Play command failed:\n{0}", ex.Message));
            }
            finally
            {
                connection.Dispose();
                OnOperationFinished();
            }
        }

        public bool CanPlayShow
        {
            get
            {
                return false;
                return 
                    this.ShowRecordingID != null &&
                    !this.IsOperationInProgress; ;
            }
        }

        //private string GetRecordingId()
        //{
        //    if (this.ShowDetails == null ||
        //        !this.ShowDetails.ContainsKey("recordingForContentId"))
        //        return null;

        //    var recordingForContent = this.ShowDetails["recordingForContentId"] as IDictionary<string, object>;

        //    if (recordingForContent == null ||
        //        !recordingForContent.ContainsKey("recordingId"))
        //        return null;

        //    return recordingForContent["recordingId"] as string;
        //}
    }
}
