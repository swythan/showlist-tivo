using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoAhoy.Phone.ViewModels
{
    public class OfferViewModel : PropertyChangedBase
    {
        private readonly INavigationService navigationService;

        private Channel channel;
        
        private Offer offer;

        public OfferViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public Channel Channel
        {
            get
            {
                return this.channel;
            }
            set
            {
                this.channel = value;
                this.NotifyOfPropertyChange(() => this.Channel);
            }   
        }

        public Offer Offer
        {
            get { return this.offer;}
            set
            {
                this.offer = value;
                this.NotifyOfPropertyChange(() => this.Offer);
            }
        }

        public void DisplayOfferDetails()
        {
            this.navigationService
                .UriFor<ShowDetailsPageViewModel>()
                .WithParam(x => x.ShowContentID, this.Offer.ContentId)
                .WithParam(x => x.ShowOfferID, this.Offer.OfferId)
                .Navigate();
        }
    }
}
