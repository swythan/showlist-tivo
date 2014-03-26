//-----------------------------------------------------------------------
// <copyright file="UpcomingOffersViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Services;

namespace TivoAhoy.Common.ViewModels
{
    public class UpcomingOffersViewModel : Screen
    {
        private readonly IProgressService progressService;
        private readonly INavigationService navigationService;
        private readonly IScheduledRecordingsService scheduledRecordingsService;
        private readonly ITivoConnectionService connectionService;
        private readonly Func<OfferViewModel> offerViewModelFactory;

        private string collectionID;
        private List<OfferViewModel> offers;

        private string contentId;
        private IEnumerable<string> excludedOfferIds;

        private bool isLoading;

        public UpcomingOffersViewModel(
            IProgressService progressService,
            INavigationService navigationService,
            IScheduledRecordingsService scheduledRecordingsService,
            ITivoConnectionService connectionService,
            Func<OfferViewModel> offerViewModelFactory)
        {
            this.progressService = progressService;
            this.navigationService = navigationService;
            this.scheduledRecordingsService = scheduledRecordingsService;
            this.connectionService = connectionService;
            this.offerViewModelFactory = offerViewModelFactory;

            connectionService.PropertyChanged += OnConnectionServicePropertyChanged;
        }

        private void OnConnectionServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
            {
                NotifyOfPropertyChange(() => this.CanRefreshOffers);

                if (this.IsActive)
                {
                    this.RefreshOffers();
                }
            }
        }

        public UpcomingOffersViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            SetOffers(
                new List<OfferViewModel> 
                { 
                    OfferViewModel.CreateDesignTime(
                        new Channel() 
                        { 
                            ChannelNumber = 101, 
                            CallSign = "BBC 1", 
                            LogoIndex = 65736 
                        }, 
                        new Offer() 
                        {
                            Title = "Antiques Roadshow", 
                            Subtitle = "Hereford", 
                            StartTime = DateTime.Parse("17:00"), 
                            DurationSeconds = 1800 
                        }) 
                });
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            NotifyOfPropertyChange(() => this.CanRefreshOffers);

            if (!this.HasOffers)
            {
                this.RefreshOffers();
            }
        }

        public string CollectionID
        {
            get
            {
                return this.collectionID;
            }
            set
            {
                if (this.collectionID == value)
                {
                    return;
                }

                this.collectionID = value;

                this.NotifyOfPropertyChange(() => this.CollectionID);

                if (this.CanRefreshOffers)
                {
                    RefreshOffers();
                }
            }
        }

        public string ContentID
        {
            get
            {
                return this.contentId;
            }
            set
            {
                if (this.contentId == value)
                {
                    return;
                }

                this.contentId = value;

                this.NotifyOfPropertyChange(() => this.ContentID);

                if (this.CanRefreshOffers)
                {
                    RefreshOffers();
                }
            }
        }

        public IEnumerable<string> ExcludedOfferIds
        {
            get { return this.excludedOfferIds; }
            set
            {
                this.excludedOfferIds = value;
                NotifyOfPropertyChange(() => this.ExcludedOfferIds);
                NotifyOfPropertyChange(() => this.Offers);
            }
        }

        public void SetOffers(List<OfferViewModel> value)
        {
            if (this.offers == value)
            {
                return;
            }
            this.offers = value;
            this.NotifyOfPropertyChange(() => this.Offers);
            this.NotifyOfPropertyChange(() => this.HasOffers);
        }

        public bool IsLoading
        {
            get { return this.isLoading; }
            set
            {
                isLoading = value;
                NotifyOfPropertyChange(() => this.StatusText);
            }
        }

        public List<OfferViewModel> Offers
        {
            get
            {
                if (this.ExcludedOfferIds == null ||
                    this.offers == null)
                {
                    return this.offers;
                }

                return this.offers.Where(x => !this.ExcludedOfferIds.Contains(x.Offer.OfferId)).ToList();
            }
        }

        public bool HasOffers
        {
            get
            {
                return
                    this.Offers != null &&
                    this.Offers.Any();
            }
        }

        public string StatusText
        {
            get
            {
                if (this.IsLoading)
                {
                    return "Loading...";
                }

                if (!this.HasOffers)
                {
                    return "No upcoming showings";
                }

                return null;
            }
        }

        public bool CanRefreshOffers
        {
            get
            {
                return this.connectionService.IsConnected;
            }
        }

        public async void RefreshOffers()
        {
            if (this.CanRefreshOffers)
            {
                await FetchOffers();
            }
        }

        private async Task FetchOffers()
        {
            try
            {
                this.IsLoading = true;

                using (this.progressService.Show())
                {
                    var connection = this.connectionService.Connection;
                    if (connection == null)
                    {
                        return;
                    }

                    List<Offer> upcomingOffers = new List<Offer>();

                    int pageSize = 25;
                    IList<Offer> page;

                    do
                    {
                        if (this.ContentID == null)
                        {
                            page = await connection.GetUpcomingOffersForCollection(this.CollectionID, upcomingOffers.Count, pageSize);
                        }
                        else
                        {
                            page = await connection.GetUpcomingOffersForContent(this.ContentID, upcomingOffers.Count, pageSize);
                        }

                        upcomingOffers.AddRange(page);
                    }
                    while (page != null && page.Count == pageSize);

                    Debug.WriteLine("Fetched {0} upcoming offers", upcomingOffers.Count);

                    this.SetOffers(upcomingOffers.Select(x => CreateOfferViewModel(x)).ToList());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Fetching upcoming shows failed :\n{0}", ex.Message));
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        private OfferViewModel CreateOfferViewModel(Offer offer)
        {
            var model = this.offerViewModelFactory();
            model.Initialise(offer);

            return model;
        }

        public void DisplayOfferDetails(OfferViewModel offer)
        {
            if (offer == null ||
                offer.Offer == null ||
                offer.Offer.ContentId == null ||
                offer.Offer.OfferId == null)
            {
                return;
            }

            var scheduledRecording = this.scheduledRecordingsService.GetScheduledRecordingForOffer(offer.Offer.OfferId);

            this.navigationService
                .UriFor<ShowDetailsPageViewModel>()
                .WithParam(x => x.ShowContentID, offer.Offer.ContentId)
                .WithParam(x => x.ShowOfferID, offer.Offer.OfferId)
                .WithParam(x => x.ShowRecordingID, scheduledRecording == null ? null : scheduledRecording.RecordingId)
                .Navigate();
        }
    }
}
