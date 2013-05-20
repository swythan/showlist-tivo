using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using Microsoft;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Events;

namespace TivoAhoy.Common.ViewModels
{
    public class ShowContainerViewModel : RecordingFolderItemViewModel<Container>
    {
        private readonly INavigationService navigationService;

        public ShowContainerViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public ShowContainerViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.Source =
                new Container()
                {
                    Title = "64 Zoo Lane",
                    FolderItemCount = 4,
                    FolderType = "series"
                };
        }

        public override bool IsSingleShow
        {
            get { return false; }
        }

        public string ContentInfo
        {
            get
            {
                if (string.Equals(this.Source.FolderType, "series", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Format("{0} episodes", this.Source.FolderItemCount);
                }
                else
                {
                    return string.Format("{0} shows", this.Source.FolderItemCount);
                }
            }
        }

        public void DisplayContainerShows()
        {
            if (this.Source == null)
                return;

            this.navigationService
                .UriFor<ShowContainerShowsPageViewModel>()
                .WithParam(x => x.ParentId, this.Source.Id)
                .WithParam(x => x.Title, this.Title)
                .Navigate();
        }
    }
}
