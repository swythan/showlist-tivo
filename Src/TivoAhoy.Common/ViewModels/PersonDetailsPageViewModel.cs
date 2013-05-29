using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Services;
using TivoAhoy.Common.Utils;

namespace TivoAhoy.Common.ViewModels
{
    public class PersonDetailsPageViewModel : Screen
    {
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;
        private int panoramaHeight = 800;

        private Person personDetails;

        private ImageBrush mainImageBrush = null;

        public PersonDetailsPageViewModel(
            IProgressService progressService,
            ITivoConnectionService connectionService)
        {
            this.progressService = progressService;
            this.connectionService = connectionService;
        }

        public PersonDetailsPageViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            //this.ShowContentID = "fakeContent";
            //this.ShowRecordingID = "fakeRecording";

            //this.Show = new ShowDetails()
            //{
            //    Title = "The Walking Dead",
            //    Subtitle = "An Interesting Episode",
            //    SeasonNumber = 2,
            //    EpisodeNumbers = new List<int>() { 3 },
            //    OriginalAirDate = new DateTime(2012, 11, 16),
            //    Images = new List<ImageInfo>
            //    {
            //        new ImageInfo()
            //        {
            //            Height= 270,
            //            Width = 360,
            //            OriginalImageUrl= "http://10.185.116.1:8080/images/os/banner-270/55/12/551290d0c77e87f7dbb1ca3db42e3b3f.jpg"
            //        }
            //    },
            //    Description = "This is the description of this very interesting episode. Don't let it catch you out, as it really is very interesting"

            //};
        }

        public int PanoramaHeight
        {
            get
            {
                return this.panoramaHeight;
            }
            set
            {
                if (this.panoramaHeight == value)
                {
                    return;
                }

                this.panoramaHeight = value;
                NotifyOfPropertyChange(() => this.PanoramaHeight);
            }
        }

        public string PersonID { get; set; }

        private async void UpdateBackgroundBrush()
        {
            this.MainImageBrush = null;

            if (Execute.InDesignMode)
            {
                return;
            }

            if (this.Person == null)
            {
                return;
            }

            int requiredHeight = this.PanoramaHeight;

            var bestImage = this.Person.Images.GetBestImageForHeight(requiredHeight);

            this.MainImageBrush = await bestImage.GetResizedImageBrushAsync(requiredHeight);
        }

        public ImageBrush MainImageBrush
        {
            get { return this.mainImageBrush; }
            set
            {
                if (this.mainImageBrush == value)
                {
                    return;
                }

                this.mainImageBrush = value;
                this.NotifyOfPropertyChange(() => MainImageBrush);
            }
        }

        public Person Person
        {
            get { return this.personDetails; }
            set
            {
                this.personDetails = value;

                Debug.WriteLine("Person details fetched:");

                NotifyOfPropertyChange(() => this.Person);
                NotifyOfPropertyChange(() => this.MainImageBrush);

                UpdateBackgroundBrush();
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            FetchPersonDetails();
        }

        private async void FetchPersonDetails()
        {
            try
            {
                using (this.progressService.Show())
                {
                    var connection = await this.connectionService.GetConnectionAsync();

                    this.Person = await connection.GetPersonDetails(this.PersonID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to retrieve person details:\n{0}", ex.Message));
            }
        }
    }
}
