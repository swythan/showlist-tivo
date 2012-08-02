using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

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

        public ShowDetails Show 
        {
            get { return this.showDetails; }
            set
            {
                this.showDetails = value;

                ////var json = value.JsonText;
                ////Debug.WriteLine("Show details fetched:");

                ////foreach (var line in json.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
                ////{
                ////    Debug.WriteLine(line);
                ////}

                Debug.WriteLine(string.Empty);

                NotifyOfPropertyChange(() => this.Show);
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

        private void FetchShowDetails()
        {
            var connection = new TivoConnection(sterlingInstance.Database);

            OnOperationStarted();
            connection.Connect(this.settingsModel.ParsedIPAddress, this.settingsModel.MediaAccessKey)
                .SelectMany(_ => connection.GetShowContentDetails(this.ShowContentID))
                .Finally(
                    () => 
                    {
                        connection.Dispose();
                        OnOperationFinished();
                    })
                .ObserveOnDispatcher()
                .Subscribe(
                    show => this.Show = show,
                    ex => MessageBox.Show(string.Format("Failed to retrieve details:\n{0}", ex.Message)));
        }

        public void PlayShow()
        {
            var connection = new TivoConnection(sterlingInstance.Database);

            OnOperationStarted();
            connection.Connect(this.settingsModel.ParsedIPAddress, this.settingsModel.MediaAccessKey)
                .SelectMany(_ => connection.PlayShow(this.ShowRecordingID))
                .Finally(
                    () =>
                    {
                        connection.Dispose();
                        OnOperationFinished();
                    })
                .ObserveOnDispatcher()
                .Subscribe(
                    show => { },
                    ex => MessageBox.Show(string.Format("Play command failed:\n{0}", ex.Message)));
        }

        public bool CanPlayShow
        {
            get
            {
                return this.ShowRecordingID != null && !this.IsOperationInProgress;
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
