using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using Coding4Fun.Toolkit.Controls;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Services;
using TivoAhoy.Common.Utils;

namespace TivoAhoy.Common.ViewModels
{
    public class PersonDetailsPageViewModel : Screen
    {
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;
        private readonly PersonContentViewModel contentSummaryViewModel;

        private int panoramaHeight = 800;

        private Person personDetails;

        private ImageBrush mainImageBrush = null;

        public PersonDetailsPageViewModel(
            IProgressService progressService,
            ITivoConnectionService connectionService,
            PersonContentViewModel contentViewModel)
        {
            this.progressService = progressService;
            this.connectionService = connectionService;
            this.contentSummaryViewModel = contentViewModel;
        }

        public PersonDetailsPageViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.PersonID = "fakePerson";
            this.Person = new Person
            {
                FirstName = "Ted",
                OriginalImageUrl = "http://10.185.116.1:8080/images/legacy/small/69/55/695556e67a7b88608fc01a5c72501a24.jpg",
                LastName = "Danson",
                BirthDate = new DateTime(1963, 2, 25),
                BirthPlace = "Los Angeles, California",
                Roles = 
                    new List<string> { "actor" },
                Images = 
                    new List<ImageInfo>
                    {
                        new ImageInfo
                        {
                            Width= 59,
                            Height = 78,
                            ImageId = "tivo:im.458355819",
                            ImageType= "person",
                            OriginalImageUrl= "http://10.185.116.1:8080/images/legacy/small/69/55/695556e67a7b88608fc01a5c72501a24.jpg",
                        },
                    }
            };
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

        public PersonContentViewModel ContentSummary
        {
            get
            {
                if (this.Person == null)
                {
                    this.contentSummaryViewModel.ContentList = null;
                }
                else
                {
                    this.contentSummaryViewModel.ContentList = this.Person.ContentSummary;
                }

                return this.contentSummaryViewModel;
            }
        }

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
                NotifyOfPropertyChange(() => this.ContentSummary);
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

                    this.Person = await connection.GetPersonDetails(this.PersonID, false);

                    this.Person = await connection.GetPersonDetails(this.PersonID, true);
                }
            }
            catch (Exception ex)
            {
                Execute.BeginOnUIThread(() =>
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Failed to fetch person details",
                        Message = ex.Message,
                        TextOrientation = Orientation.Vertical,
                        TextWrapping = TextWrapping.Wrap,
                        Background = new SolidColorBrush(Colors.Red),
                    };

                    toast.Show();
                });
            }
        }
    }
}
