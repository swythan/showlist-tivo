//-----------------------------------------------------------------------
// <copyright file="ShowDetailsPageViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Coding4Fun.Toolkit.Controls;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Services;
using TivoAhoy.Common.Utils;

namespace TivoAhoy.Common.ViewModels
{
    public class ShowDetailsPageViewModel : Screen
    {
        private readonly IAnalyticsService analyticsService;
        private readonly INavigationService navigationService;
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;
        private readonly CreditsViewModel creditsViewModel;
        private readonly UpcomingOffersViewModel upcomingOffersViewModel;

        private ShowDetails showDetails;
        private Recording recordingDetails;
        private Offer offerDetails;

        private bool isOperationInProgress;
        private int panoramaHeight = 800;

        private ImageBrush mainImageBrush = null;

        public ShowDetailsPageViewModel(
            IAnalyticsService analyticsService,
            INavigationService navigationService,
            IProgressService progressService,
            ITivoConnectionService connectionService,
            CreditsViewModel creditsViewModel,
            UpcomingOffersViewModel upcomingOffersViewModel)
        {
            this.analyticsService = analyticsService;
            this.navigationService = navigationService;
            this.progressService = progressService;
            this.connectionService = connectionService;
            this.creditsViewModel = creditsViewModel;
            this.upcomingOffersViewModel = upcomingOffersViewModel;
        }

        public ShowDetailsPageViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.ShowContentID = "fakeContent";
            this.ShowRecordingID = "fakeRecording";

            this.Show = new ShowDetails()
            {
                Title = "The Walking Dead",
                Subtitle = "An Interesting Episode",
                SeasonNumber = 2,
                EpisodeNumbers = new List<int>() { 3 },
                OriginalAirDate = new DateTime(2012, 11, 16),
                Images = new List<ImageInfo>
                {
                    new ImageInfo()
                    {
                        Height= 270,
                        Width = 360,
                        OriginalImageUrl= "http://10.185.116.1:8080/images/os/banner-270/55/12/551290d0c77e87f7dbb1ca3db42e3b3f.jpg"
                    }
                },
                Description = "This is the description of this very interesting episode. Don't let it catch you out, as it really is very interesting"
            };

            this.Offer =
                new Offer()
                {
                    Title = "The Walking Dead",
                    Subtitle = "An Interesting Episode",
                    StartTime = DateTime.Parse("21:00"),
                    DurationSeconds = 1825,
                    Channel =
                        new Channel()
                        {
                            ChannelNumber = 150,
                            CallSign = "Ch5 HD",
                            LogoIndex = 65736
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
            get
            {
                return this.isOperationInProgress;
            }
        }

        private void SetIsOperationInProgress(bool value)
        {
            if (this.isOperationInProgress == value)
            {
                return;
            }

            this.isOperationInProgress = value;

            NotifyOfPropertyChange(() => this.IsOperationInProgress);
            NotifyOfPropertyChange(() => this.CanPlayShow);
            NotifyOfPropertyChange(() => this.CanDeleteShow);
            NotifyOfPropertyChange(() => this.CanCancelRecording);
            NotifyOfPropertyChange(() => this.CanScheduleRecording);
        }

        private IDisposable ShowProgress()
        {
            this.SetIsOperationInProgress(true);

            return new CompositeDisposable(
                this.progressService.Show(),
                Disposable.Create(() => this.SetIsOperationInProgress(false)));
        }

        public string ShowContentID { get; set; }
        public string ShowRecordingID { get; set; }
        public string ShowOfferID { get; set; }

        public bool HasSubtitle
        {
            get
            {
                return
                    this.Show != null &&
                    !string.IsNullOrWhiteSpace(this.Show.Subtitle);
            }
        }

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

        private async void UpdateBackgroundBrush()
        {
            this.MainImageBrush = null;

            if (Execute.InDesignMode)
            {
                return;
            }

            if (this.Show == null)
            {
                return;
            }

            int requiredHeight = this.PanoramaHeight;

            var bestImage = this.Show.Images.GetBestImageForHeight(requiredHeight);

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

        public ShowDetails Show
        {
            get { return this.showDetails; }
            set
            {
                this.showDetails = value;

                Debug.WriteLine("Show details fetched:");

                NotifyOfPropertyChange(() => this.Show);
                NotifyOfPropertyChange(() => this.MainImageBrush);
                NotifyOfPropertyChange(() => this.Credits);
                NotifyOfPropertyChange(() => this.HasSubtitle);
                NotifyOfPropertyChange(() => this.HasEpisodeNumbers);
                NotifyOfPropertyChange(() => this.HasOriginalAirDate);

                UpdateBackgroundBrush();
            }
        }

        public Recording Recording
        {
            get { return this.recordingDetails; }
            set
            {
                this.recordingDetails = value;

                Debug.WriteLine("Recording details fetched:");

                NotifyOfPropertyChange(() => this.Recording);
                NotifyOfPropertyChange(() => this.IsRecorded);
                NotifyOfPropertyChange(() => this.IsRecordable);
                NotifyOfPropertyChange(() => this.IsScheduled);
                NotifyOfPropertyChange(() => this.CanDeleteShow);
                NotifyOfPropertyChange(() => this.CanPlayShow);
                NotifyOfPropertyChange(() => this.CanCancelRecording);
                NotifyOfPropertyChange(() => this.CanScheduleRecording);
                NotifyOfPropertyChange(() => this.Channel);
                NotifyOfPropertyChange(() => this.StartTime);
                NotifyOfPropertyChange(() => this.Duration);
            }
        }

        public Offer Offer
        {
            get { return this.offerDetails; }
            set
            {
                this.offerDetails = value;

                Debug.WriteLine("Offer details fetched:");

                NotifyOfPropertyChange(() => this.Offer);
                NotifyOfPropertyChange(() => this.IsRecordable);
                NotifyOfPropertyChange(() => this.CanScheduleRecording);
                NotifyOfPropertyChange(() => this.Channel);
                NotifyOfPropertyChange(() => this.StartTime);
                NotifyOfPropertyChange(() => this.Duration);
            }
        }

        public CreditsViewModel Credits
        {
            get
            {
                if (this.Show == null)
                {
                    this.creditsViewModel.Credits = null;
                }
                else
                {
                    this.creditsViewModel.Credits = this.Show.Credits;
                }

                return this.creditsViewModel;
            }
        }

        public UpcomingOffersViewModel UpcomingOffers
        {
            get
            {
                this.upcomingOffersViewModel.ContentID = this.ShowContentID;
                this.upcomingOffersViewModel.ExcludedOfferIds = new[] { this.ShowOfferID };

                return this.upcomingOffersViewModel;
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            FetchShowDetails();
        }

        private async void FetchShowDetails()
        {
            try
            {
                var connection = this.connectionService.Connection;
                if (connection == null)
                {
                    return;
                }

                using (this.ShowProgress())
                {
                    this.Show = await connection.GetShowContentDetails(this.ShowContentID);

                    if (!string.IsNullOrEmpty(this.ShowOfferID))
                    {
                        this.Offer = await connection.GetOfferDetails(this.ShowOfferID);
                    }

                    if (!string.IsNullOrEmpty(this.ShowRecordingID))
                    {
                        this.Recording = await connection.GetRecordingDetails(this.ShowRecordingID);
                    }
                }
            }
            catch (Exception ex)
            {
                Execute.BeginOnUIThread(() =>
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Failed to fetch show details",
                        Message = ex.Message,
                        TextOrientation = Orientation.Vertical,
                        TextWrapping = TextWrapping.Wrap,
                        Background = new SolidColorBrush(Colors.Red),
                    };

                    toast.Show();
                });
            }
        }

        public bool IsRecorded
        {
            get
            {
                if (Execute.InDesignMode)
                    return true;

                if (this.Recording == null)
                    return false;

                if (this.Recording.State != "complete" &&
                    this.Recording.State != "inProgress")
                    return false;

                return true;
            }
        }

        public bool IsScheduled
        {
            get
            {
                if (Execute.InDesignMode)
                    return true;

                if (this.Recording == null)
                    return false;

                return
                    this.Recording.State == "scheduled";
            }
        }

        public bool IsRecordable
        {
            get
            {
                if (Execute.InDesignMode)
                    return true;

                if (this.ShowOfferID == null)
                    return false;

                if (this.ShowRecordingID == null)
                    return true;

                if (this.Recording == null)
                    return false;

                // TODO : Should we be able to record existing recordings in certain states (e.g. cancelled)?
                return false;
            }
        }

        public Channel Channel
        {
            get
            {
                if (this.Offer != null)
                {
                    return this.Offer.Channel;
                }

                if (this.Recording != null)
                {
                    return this.Recording.Channel;
                }

                return null;
            }
        }

        public DateTime? StartTime
        {
            get
            {
                if (this.Recording != null)
                {
                    return this.Recording.ScheduledStartTime;
                }

                if (this.Offer != null)
                {
                    return this.Offer.StartTime;
                }

                return null;
            }
        }

        public TimeSpan? Duration
        {
            get
            {
                if (this.Recording != null)
                {
                    return this.Recording.ScheduledEndTime - this.Recording.ScheduledStartTime;
                }

                if (this.Offer != null)
                {
                    return this.Offer.Duration;
                }

                return null;
            }
        }

        public bool CanPlayShow
        {
            get
            {
                if (!this.IsRecorded)
                    return false;

                return
                    this.connectionService.IsHomeMode &&
                    !this.IsOperationInProgress;
            }
        }

        public async void PlayShow()
        {
            this.analyticsService.PlayRecording();

            try
            {
                var connection = this.connectionService.Connection;
                if (connection == null)
                {
                    return;
                }

                using (this.ShowProgress())
                {
                    await connection.PlayShow(this.ShowRecordingID);
                }
            }
            catch (Exception ex)
            {
                Execute.BeginOnUIThread(() =>
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Play command failed",
                        Message = ex.Message,
                        TextOrientation = Orientation.Vertical,
                        TextWrapping = TextWrapping.Wrap,
                        Background = new SolidColorBrush(Colors.Red),
                    };

                    toast.Show();
                });
            }
        }

        public bool CanDeleteShow
        {
            get
            {
                if (this.IsOperationInProgress)
                    return false;

                if (this.Recording == null)
                    return false;

                return this.Recording.State == "complete";
            }
        }

        public async void DeleteShow()
        {
            this.analyticsService.DeleteRecording();

            try
            {
                var connection = this.connectionService.Connection;
                if (connection == null)
                {
                    return;
                }

                using (this.ShowProgress())
                {
                    await connection.DeleteRecording(this.ShowRecordingID);
                }

                var toast = new ToastPrompt()
                {
                    Title = "Recording Deleted",
                    TextOrientation = Orientation.Vertical,
                };

                toast.Show();
            }
            catch (Exception ex)
            {
                Execute.BeginOnUIThread(() =>
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Delete command failed",
                        Message = ex.Message,
                        TextOrientation = Orientation.Vertical,
                        TextWrapping = TextWrapping.Wrap,
                        Background = new SolidColorBrush(Colors.Red),
                    };

                    toast.Show();
                });
            }
        }

        public bool CanCancelRecording
        {
            get
            {
                if (this.IsOperationInProgress)
                    return false;

                if (!this.IsScheduled)
                    return false;

                return true;
            }
        }

        public async void CancelRecording()
        {
            this.analyticsService.CancelSingleRecording();

            try
            {
                var connection = this.connectionService.Connection;
                if (connection == null)
                {
                    return;
                }

                using (this.ShowProgress())
                {
                    await connection.CancelRecording(this.ShowRecordingID);
                }

                var toast = new ToastPrompt()
                {
                    Title = "Recording Cancelled",
                    TextOrientation = Orientation.Vertical,
                };

                toast.Show();
            }
            catch (Exception ex)
            {
                Execute.BeginOnUIThread(() =>
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Cancel Recording Failed",
                        Message = ex.Message,
                        TextOrientation = Orientation.Vertical,
                        TextWrapping = TextWrapping.Wrap,
                        Background = new SolidColorBrush(Colors.Red),
                    };

                    toast.Show();
                });
            }
        }

        public bool CanScheduleRecording
        {
            get
            {
                if (!this.IsRecordable)
                    return false;

                if (this.IsScheduled)
                    return false;

                return !this.IsOperationInProgress;
            }
        }

        public async void ScheduleRecording()
        {
            this.analyticsService.ScheduleSingleRecording();

            try
            {
                var connection = this.connectionService.Connection;
                if (connection == null)
                {
                    return;
                }

                SubscribeResult result;
                using (this.ShowProgress())
                {
                    result = await connection.ScheduleSingleRecording(this.ShowContentID, this.ShowOfferID);
                }

                if (result.Subscription != null)
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Recording Scheduled",
                        TextOrientation = Orientation.Vertical,
                    };

                    toast.Show();
                }
                else
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Schedule Recording Failed",
                        Message = "Could not schedule recording due to conflicts.",
                        TextOrientation = Orientation.Vertical,
                        TextWrapping = TextWrapping.Wrap,
                        Background = new SolidColorBrush(Colors.Red),
                    };

                    toast.Show();
                }
            }
            catch (Exception ex)
            {
                Execute.BeginOnUIThread(() =>
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Schedule Recording Failed",
                        Message = ex.Message,
                        TextOrientation = Orientation.Vertical,
                        TextWrapping = TextWrapping.Wrap,
                        Background = new SolidColorBrush(Colors.Red),
                    };

                    toast.Show();
                });
            }
        }

        public void DisplayCollectionDetails()
        {
            if (this.Show == null ||
                this.Show.CollectionId == null)
            {
                return;
            }

            this.navigationService
                .UriFor<CollectionDetailsPageViewModel>()
                .WithParam(x => x.CollectionID, this.Show.CollectionId)
                .Navigate();
        }
    }
}
