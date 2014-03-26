//-----------------------------------------------------------------------
// <copyright file="OfferViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Events;
using TivoAhoy.Common.Services;

namespace TivoAhoy.Common.ViewModels
{
    public class OfferViewModel : PropertyChangedBase
    {
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;
        private readonly IScheduledRecordingsService scheduledRecordingsService;

        private Channel channel;
        private DateTime time;

        private Task offerTask;
        private Offer offer;

        public OfferViewModel(
            ITivoConnectionService connectionService,
            IProgressService progressService,
            IScheduledRecordingsService scheduledRecordingsService)
        {
            this.connectionService = connectionService;
            this.progressService = progressService;
            this.scheduledRecordingsService = scheduledRecordingsService;

            if (this.scheduledRecordingsService != null)
            {
                this.scheduledRecordingsService.PropertyChanged += OnRecordingScheduleUpdated;
            }
        }

        private void OnRecordingScheduleUpdated(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ScheduledRecordings")
            {
                this.NotifyOfPropertyChange(() => this.IsRecordingScheduled);
            }
        }

        public void Initialise(Channel channel, DateTime time)
        {
            this.channel = channel;
            this.time = time;
        }

        public void Initialise(Offer offer)
        {
            this.channel = offer.Channel;
            this.offer = offer;
        }

        public static OfferViewModel CreateDesignTime(Channel channel, Offer offer)
        {
            var model = new OfferViewModel(null, null, null);

            model.channel = channel;
            model.offer = offer;

            return model;
        }

        public Channel Channel
        {
            get
            {
                return this.channel;
            }
        }

        public Offer Offer
        {
            get
            {
                if (offer != null)
                {
                    return this.offer;
                }

                if (this.offerTask == null)
                {
                    this.offerTask = UpdateOfferAsync();
                }

                return null;
            }

            private set
            {
                this.offer = value;
                this.NotifyOfPropertyChange(() => this.Offer);
                this.NotifyOfPropertyChange(() => this.UpcomingOfferDisplayText);
                this.NotifyOfPropertyChange(() => this.IsRecordingScheduled);
            }
        }

        public bool IsRecordingScheduled
        {
            get
            {
                if (this.scheduledRecordingsService == null)
                {
                    return true;
                }

                if (this.Offer == null)
                {
                    return false;
                }

                return this.scheduledRecordingsService.IsOfferRecordingScheduled(this.Offer.OfferId);
            }
        }

        public string UpcomingOfferDisplayText
        {
            get
            {
                if (this.Offer == null)
                {
                    return null;
                }

                var bestTitle = string.IsNullOrWhiteSpace(this.Offer.Subtitle) ? this.Offer.Title : this.Offer.Subtitle;

                if (this.Offer.SeasonNumber != null &&
                    this.Offer.EpisodeNumbers != null)
                {
                    return string.Format("{0} [S{1} E{2}]", bestTitle, this.Offer.SeasonNumber, this.Offer.EpisodeNumberText); 
                }

                return bestTitle;
            }
        }

        private async Task UpdateOfferAsync()
        {
            try
            {
                var connection = this.connectionService.Connection;
                if (connection == null)
                {
                    return;
                }

                using (this.progressService.Show())
                {
                    var rows = await connection.GetGridShowsAsync(
                        this.time,
                        this.time,
                        this.Channel.ChannelNumber,
                        1,
                        0);

                    if (rows.Count > 0)
                    {
                        this.Offer = rows[0].Offers.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error fetching now showing for channel {0} : {1}", this.Channel.ChannelNumber, ex);
            }
        }
    }
}
