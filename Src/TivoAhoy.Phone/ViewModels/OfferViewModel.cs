using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoAhoy.Phone.ViewModels
{
    public class OfferViewModel : PropertyChangedBase
    {
        private readonly ITivoConnectionService connectionService;

        private Channel channel;
        private DateTime time;

        private Task offerTask;
        private Offer offer;

        public OfferViewModel(ITivoConnectionService connectionService)
        {
            this.connectionService = connectionService;
        }

        public void Initialise(Channel channel, DateTime time)
        {
            this.channel = channel;
            this.time = time;
        }

        public static OfferViewModel CreateDesignTime(Channel channel, Offer offer)
        {
            var model = new OfferViewModel(null);

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
            }
        }

        private async Task UpdateOfferAsync()
        {
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
        }
    }
}
