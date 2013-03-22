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
        private readonly Channel channel;
        
        private Offer offer;

        public OfferViewModel(Channel channel)
        {
            this.channel = channel;
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
            get { return this.offer;}
            set
            {
                this.offer = value;
                this.NotifyOfPropertyChange(() => this.Offer);
            }
        }
    }
}
