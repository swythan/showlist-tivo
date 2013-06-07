using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace TivoAhoy.PhoneRT.Views
{
    public partial class PersonDetailsPage : PhoneApplicationPage
    {
        public PersonDetailsPage()
        {
            InitializeComponent();
        }
        private void Panorama_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var model = this.DataContext as TivoAhoy.Common.ViewModels.PersonDetailsPageViewModel;

            if (model != null)
            {
                model.PanoramaHeight = Convert.ToInt32(e.NewSize.Height);
            }
        }
    }
}