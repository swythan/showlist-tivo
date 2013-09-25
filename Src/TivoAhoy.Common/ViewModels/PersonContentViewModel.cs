//-----------------------------------------------------------------------
// <copyright file="PersonContentViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoAhoy.Common.ViewModels
{
    public class PersonContentViewModel : Screen
    {
        private readonly INavigationService navigationService;
        private List<ContentSummaryForPersonId> content;

        public PersonContentViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public PersonContentViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            ContentList = 
                new List<ContentSummaryForPersonId>
                {
                    new ContentSummaryForPersonId()
                    {
                        Title = "CSI: Crime Scene Investigation",
                        Images = new List<ImageInfo>
                        {
                            new ImageInfo()
                            {
                                Height= 270,
                                Width = 360,
                                OriginalImageUrl= "http://10.185.116.1:8080/images/os/banner-270/b9/4b/b94b1843d277920b702edef44a8ce272.jpg"
                            }
                        },
                    }
                };
        }

        public List<ContentSummaryForPersonId> ContentList 
        {
            get { return this.content; }

            set
            {
                if (this.content == value)
                {
                    return;
                }

                this.content = value;
                this.NotifyOfPropertyChange(() => this.ContentList);
            }
        }

        public bool HasContent
        {
            get
            {
                return
                    this.ContentList != null &&
                    this.ContentList.Any();
            }
        }

        public void DisplayContentDetails(ContentSummaryForPersonId contentSummary)
        {
            if (contentSummary == null ||
                contentSummary.CollectionId == null)
            {
                return;
            }

            this.navigationService
                .UriFor<CollectionDetailsPageViewModel>()
                .WithParam(x => x.CollectionID, contentSummary.CollectionId)
                .Navigate();
        }

    }
}
