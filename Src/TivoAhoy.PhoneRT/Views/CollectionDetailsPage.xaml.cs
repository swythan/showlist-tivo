using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            }
        }

        void OnModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Credits")
            {
                var model = this.DataContext as TivoAhoy.Common.ViewModels.CollectionDetailsPageViewModel;

                if (model != null)
                {
                    this.Credits.Visibility = model.Credits.HasCredits ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
    }
}