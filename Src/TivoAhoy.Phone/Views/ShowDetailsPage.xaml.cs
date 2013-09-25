//-----------------------------------------------------------------------
// <copyright file="ShowDetailsPage.xaml.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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

namespace TivoAhoy.Phone.Views
{
    public partial class ShowDetailsPage : PhoneApplicationPage
    {
        public ShowDetailsPage()
        {
            InitializeComponent();
        }

        private void Panorama_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var model = this.DataContext as TivoAhoy.Common.ViewModels.ShowDetailsPageViewModel;

            if (model != null)
            {
                model.PanoramaHeight = Convert.ToInt32(e.NewSize.Height);
            }
        }
    }
}
