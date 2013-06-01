using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace TivoAhoy.PhoneRT.Views
{
    public partial class CollectionDetailsPage : PhoneApplicationPage
    {
        IDisposable creditsPropertyChangedSub;
        IDisposable offersPropertyChangedSub;

        public CollectionDetailsPage()
        {
            InitializeComponent();
        }

        private void Panorama_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var model = this.DataContext as TivoAhoy.Common.ViewModels.CollectionDetailsPageViewModel;

            if (model != null)
            {
                model.PanoramaHeight = Convert.ToInt32(e.NewSize.Height);

                model.PropertyChanged += OnModelPropertyChanged;

                //if (model.Credits != null)
                //{
                //    model.Credits.PropertyChanged += (_, args) => this.UpdateCreditsVisibility();
                //}

                //if (model.UpcomingOffers != null)
                //{
                //    model.UpcomingOffers.PropertyChanged += (_, args) => this.UpdateUpcomingOffersVisibility();
                //}
            }
        }

        void OnModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var model = this.DataContext as TivoAhoy.Common.ViewModels.CollectionDetailsPageViewModel;

            if (e.PropertyName == "Credits")
            {
                //if (model.Credits != null)
                //{
                //    model.Credits.PropertyChanged += (_, args) => this.UpdateCreditsVisibility();
                //}

                this.UpdateCreditsVisibility();
            } 
            
            if (e.PropertyName == "UpcomingOffers")
            {
                //if (model.UpcomingOffers != null)
                //{
                //    model.UpcomingOffers.PropertyChanged += (_, args) => this.UpdateUpcomingOffersVisibility();
                //}

                //UpdateUpcomingOffersVisibility();
            }
        }

        private void UpdateUpcomingOffersVisibility()
        {
            var model = this.DataContext as TivoAhoy.Common.ViewModels.CollectionDetailsPageViewModel;

            if (model != null)
            {
                this.UpcomingOffers.Visibility = model.UpcomingOffers.HasOffers ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateCreditsVisibility()
        {
            var model = this.DataContext as TivoAhoy.Common.ViewModels.CollectionDetailsPageViewModel;

            if (model != null)
            {
                this.Credits.Visibility = model.Credits.HasCredits ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}