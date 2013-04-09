using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class OfferViewModel : PropertyChangedBase
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ITivoConnectionService connectionService;
        private readonly IScheduledRecordingsService scheduledRecordingsService;

        private Channel channel;
        private DateTime time;

        private Task offerTask;
        private Offer offer;

        public OfferViewModel(
            ITivoConnectionService connectionService,
            IEventAggregator eventAggregator,
            IScheduledRecordingsService scheduledRecordingsService)
        {
            this.connectionService = connectionService;
            this.eventAggregator = eventAggregator;
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

        private void OnOperationStarted()
        {
            this.eventAggregator.Publish(new TivoOperationStarted());
        }

        private void OnOperationFinished()
        {
            this.eventAggregator.Publish(new TivoOperationFinished());
        }

        private async Task UpdateOfferAsync()
        {
            this.OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

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
            catch (Exception ex)
            {
                Debug.WriteLine("Error fetching now showing for channel {0} : {1}", this.Channel.ChannelNumber, ex);
            }
            finally
            {
                this.OnOperationFinished();
            }
        }
    }
}
