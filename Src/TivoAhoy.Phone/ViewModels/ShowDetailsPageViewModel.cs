using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;
using System.Diagnostics;

namespace TivoAhoy.Phone.ViewModels
{
    public class ShowDetailsPageViewModel : Screen
    {
        private readonly ISterlingInstance sterlingInstance;
        private readonly SettingsPageViewModel settingsModel;

        private ShowDetails showDetails;

        public ShowDetailsPageViewModel(
            ISterlingInstance sterlingInstance, 
            SettingsPageViewModel settingsModel)
        {
            this.sterlingInstance = sterlingInstance;
            this.settingsModel = settingsModel;
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

            connection.Connect(this.settingsModel.ParsedIPAddress, this.settingsModel.MediaAccessKey)
                .SelectMany(_ => connection.GetShowContentDetails(this.ShowContentID))
                .ObserveOnDispatcher()
                .Subscribe(show => this.Show = show,
                    ex =>
                    {
                        MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
                        connection.Dispose();
                    },
                    () => connection.Dispose());
        }

        public void PlayShow()
        {
            var connection = new TivoConnection(sterlingInstance.Database);

            connection.Connect(this.settingsModel.ParsedIPAddress, this.settingsModel.MediaAccessKey)
                .SelectMany(_ => connection.PlayShow(this.ShowRecordingID))
                .ObserveOnDispatcher()
                .Subscribe(show => MessageBox.Show("Play command sent"),
                    ex =>
                    {
                        MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
                        connection.Dispose();
                    },
                    () => connection.Dispose());

        }

        public bool CanPlayShow
        {
            get
            {
                return this.ShowRecordingID != null;
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
