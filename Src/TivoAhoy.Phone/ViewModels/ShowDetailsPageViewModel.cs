using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Caliburn.Micro;
using Tivo.Connect;
using System.Reactive;
using System.Reactive.Linq;
using Tivo.Connect.Entities;
using System.Collections.Generic;

namespace TivoAhoy.Phone.ViewModels
{
    public class ShowDetailsPageViewModel : Screen
    {
        private readonly ISterlingInstance sterlingInstance;
        private readonly SettingsPageViewModel settingsModel;

        private IDictionary<string, object> showDetails;

        public ShowDetailsPageViewModel(
            ISterlingInstance sterlingInstance, 
            SettingsPageViewModel settingsModel)
        {
            this.sterlingInstance = sterlingInstance;
            this.settingsModel = settingsModel;
        }

        public string PageTitle
        {
            get
            {
                if (this.ShowDetails == null)
                {
                    return string.Empty;
                }

                return (string)this.ShowDetails["title"];
            }
        }

        public string EpisodeTitle
        {
            get
            {
                if (this.ShowDetails == null)
                {
                    return string.Empty;
                }

                return (string)this.ShowDetails["subtitle"];
            }
        }

        public string Description
        {
            get
            {
                if (this.ShowDetails == null)
                {
                    return string.Empty;
                }

                return (string)this.ShowDetails["description"];
            }
        }

        public string ShowContentID { get; set; }
        public string ShowRecordingID { get; set; }

        public IDictionary<string, object> ShowDetails 
        {
            get { return this.showDetails; }
            set
            {
                this.showDetails = value;

                NotifyOfPropertyChange(() => this.ShowDetails);
                NotifyOfPropertyChange(() => this.PageTitle);
                NotifyOfPropertyChange(() => this.EpisodeTitle);
                NotifyOfPropertyChange(() => this.Description);
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

            connection.Connect(this.settingsModel.TivoIPAddress, this.settingsModel.MediaAccessKey)
                .SelectMany(_ => connection.GetShowContentDetails(this.ShowContentID))
                .ObserveOnDispatcher()
                .Subscribe(show => this.ShowDetails = show,
                    ex =>
                    {
                        MessageBox.Show(string.Format("Connection Failed! :-(\n{0}", ex));
                        connection.Dispose();
                    },
                    () => connection.Dispose());
        }

        public void PlayShow()
        {
            var connection = new TivoConnection(sterlingInstance.Database);

            connection.Connect(this.settingsModel.TivoIPAddress, this.settingsModel.MediaAccessKey)
                .SelectMany(_ => connection.PlayShow(this.ShowRecordingID))
                .ObserveOnDispatcher()
                .Subscribe(show => MessageBox.Show("Command sent!"),
                    ex =>
                    {
                        MessageBox.Show(string.Format("Connection Failed! :-(\n{0}", ex));
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
