//-----------------------------------------------------------------------
// <copyright file="ImageInfoListToUrlConverter.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Linq;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Utils;

namespace TivoAhoy.Phone.Converters
{
    public sealed class ImageInfoListToUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var images = value as IEnumerable<ImageInfo>;
            if (images == null ||
                !images.Any())
            {
                return null;
            }

            var desiredHeight = 120;
            if (parameter is int)
            {
                desiredHeight = (int)parameter;
            }

            var bestImage = images.GetBestImageForHeight(desiredHeight);

            if (bestImage == null)
            {
                return null;
            }

            return bestImage.ImageUrl;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
