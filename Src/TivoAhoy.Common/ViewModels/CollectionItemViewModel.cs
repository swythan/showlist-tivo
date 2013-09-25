//-----------------------------------------------------------------------
// <copyright file="CollectionItemViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Linq;
using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoAhoy.Common.ViewModels
{
    public class CollectionItemViewModel : UnifiedItemViewModel<Collection>
    {
        private readonly INavigationService navigationService;

        public CollectionItemViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public CollectionItemViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.Source =
                new Collection()
                {
                    Title = "The Walking Dead",
                };
        }

        public override string Title
        {
            get
            {
                if (this.Source == null)
                    return null;

                return this.Source.Title;
            }
        }

        public override string Subtitle
        {
            get
            {
                if (this.Source == null)
                    return null;

                var categories = this.Source.Categories
                    .Select(x => x.Label)
                    .Distinct()
                    .Take(2);

                return string.Join(", ", categories);
            }
        }

        public void DisplayCollectionDetails()
        {
            this.navigationService
                .UriFor<CollectionDetailsPageViewModel>()
                .WithParam(x => x.CollectionID, this.Source.CollectionId)
                .Navigate();
        }
    }
}
