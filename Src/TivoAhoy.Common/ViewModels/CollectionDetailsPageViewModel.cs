using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Services;
using TivoAhoy.Common.Utils;

namespace TivoAhoy.Common.ViewModels
{
    public class CollectionDetailsPageViewModel : Screen
    {
        private readonly IAnalyticsService analyticsService;
        private readonly IEventAggregator eventAggregator;
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;

        private Collection collectionDetails;

        private bool isOperationInProgress;
        private int panoramaHeight = 800;

        private ImageBrush mainImageBrush = null;

        public CollectionDetailsPageViewModel(
            IAnalyticsService analyticsService,
            IEventAggregator eventAggregator,
            IProgressService progressService,
            ITivoConnectionService connectionService)
        {
            this.analyticsService = analyticsService;
            this.eventAggregator = eventAggregator;
            this.progressService = progressService;
            this.connectionService = connectionService;
        }

        public CollectionDetailsPageViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.CollectionID = "fakeContent";

            this.Collection = new Collection()
            {
                Title = "CSI: Crime Scene Investigation",
                Images = new List<ImageInfo>
                {
                    new ImageInfo()
                    {
                        Height= 270,
                        Width = 360,
                        OriginalImageUrl= "http://10.185.116.1:8080/images/os/banner-270/b9/4b/b94b1843d277920b702edef44a8ce272.jpg"
                    }
                },
                Description = "A specialised team of forensic investigators finds the missing pieces to dangerous puzzles.",
                Credits = new List<Credit>
                {
                    new Credit
                    {
                        CharacterName= "D.B. Russell",
                        FirstName ="Ted",
                        OriginalImageUrl= "http://10.185.116.1:8080/images/legacy/small/69/55/695556e67a7b88608fc01a5c72501a24.jpg",
                        LastName= "Danson",
                        PersonId= "tivo:pn.61724",
                        Role = "actor",
                        Images = new List<ImageInfo>
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
                    }
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

        public bool IsOperationInProgress
        {
            get { return this.isOperationInProgress; }
        }

        private void OnOperationStarted()
        {
            this.isOperationInProgress = true;
            NotifyOfPropertyChange(() => this.IsOperationInProgress);
        }

        private void OnOperationFinished()
        {
            this.isOperationInProgress = false;
            NotifyOfPropertyChange(() => this.IsOperationInProgress);
        }

        public string CollectionID { get; set; }

        private async void UpdateBackgroundBrush()
        {
            this.MainImageBrush = null;

            if (Execute.InDesignMode)
            {
                return;
            }

            if (this.Collection == null)
            {
                return;
            }

            int requiredHeight = this.PanoramaHeight;

            var bestImage = this.Collection.Images.GetBestImageForHeight(requiredHeight);

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

        public Collection Collection
        {
            get { return this.collectionDetails; }
            set
            {
                this.collectionDetails = value;

                Debug.WriteLine("Collection details fetched:");

                NotifyOfPropertyChange(() => this.Collection);
                NotifyOfPropertyChange(() => this.HasCredits);
                NotifyOfPropertyChange(() => this.MainImageBrush);

                UpdateBackgroundBrush();
            }
        }

        public bool HasCredits
        {
            get
            {
                return
                    this.Collection != null &&
                    this.Collection.Credits != null &&
                    this.Collection.Credits.Any();
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            FetchCollectionDetails();
        }

        private async void FetchCollectionDetails()
        {
            OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                using (progressService.Show())
                {
                    this.Collection = await connection.GetCollectionDetails(this.CollectionID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to retrieve collection details:\n{0}", ex.Message));
            }
            finally
            {
                OnOperationFinished();
            }
        }

    }
}
