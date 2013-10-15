//-----------------------------------------------------------------------
// <copyright file="ImageUrlMapper.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Tivo.Connect.Entities;

namespace Tivo.Connect
{
    public class ImageUrlMapper
    {
        private static Lazy<ImageUrlMapper> Instance = new Lazy<ImageUrlMapper>(() => new ImageUrlMapper());

        // Setup with current UK defaults so that the XAML designers work
        private string internalBase = "http://10.185.116.1:8080/images/";
        private string externalBase = "http://tivo-icdn.virginmedia.com/images-vm_production/";
        private string logoImageBase = "http://tivo-icdn.virginmedia.com/images-production/static/logos";

        private ImageUrlMapper()
        {
        }

        public static ImageUrlMapper Default
        {
            get { return Instance.Value; }
        }

        public void Initialise(IList<AppGlobalData> appData)
        {
            var imageBaseUrls = appData.Where(x => x.AppName == "imageBaseUrl");

            this.internalBase = GetItemByKey(imageBaseUrls, "internalBaseUrl");
            this.externalBase = GetItemByKey(imageBaseUrls, "externalBaseUrl");
            this.logoImageBase = GetItemByKey(imageBaseUrls, "logoImageBaseUrl");
        }

        private static string GetItemByKey(IEnumerable<AppGlobalData> data, string keyName)
        {
            return data
                .Where(x => x.KeyName == keyName)
                .Select(x => x.Value)
                .FirstOrDefault();
        }

        public Uri GetExternalImageUrl(string internalImageUrl)
        {
            if (this.internalBase == null ||
                this.externalBase == null ||
                internalImageUrl == null)
            {
                return null;
            }

            var externalUrlString = internalImageUrl.Replace(this.internalBase, this.externalBase);

            Uri externalUrl;
            Uri.TryCreate(externalUrlString, UriKind.Absolute, out externalUrl);

            return externalUrl;
        }

        public Uri GetLogoImageUrl(int? logoIndex)
        {
            if (this.logoImageBase == null ||
                logoIndex == null)
            {
                return null;
            }

            int logoIdInUrl = logoIndex.Value & 0xFFFF;
            var logoUrlString = string.Format("{0}/65x55/{1}.png", this.logoImageBase, logoIdInUrl);

            Uri logoUrl;
            Uri.TryCreate(logoUrlString, UriKind.Absolute, out logoUrl);

            return logoUrl;
        }
    }
}
