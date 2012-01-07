using System.Collections.ObjectModel;
using Caliburn.Micro;
using System;
using Tivo.Connect;
using System.Windows;
using System.Collections.Generic;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.ViewModels;

namespace TivoAhoy.Phone.ViewModels
{
    public class MainPageViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private readonly INavigationService navigationService;

        public MainPageViewModel(INavigationService navigationService, MyShowsViewModel myShowsViewModel)
        {
            this.navigationService = navigationService;

            myShowsViewModel.DisplayName = "my shows";
            this.Items.Add(myShowsViewModel);

            this.ActivateItem(myShowsViewModel);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            NotifyOfPropertyChange(() => this.CanRefreshList);
        }

        protected override void OnActivationProcessed(IScreen item, bool success)
        {
            base.OnActivationProcessed(item, success);
            NotifyOfPropertyChange(() => this.CanRefreshList);
        }

        public void ShowSettings()
        {
            navigationService.UriFor<SettingsPageViewModel>().Navigate();
        }

        public bool CanRefreshList
        {
            get
            {
                var myShows = this.ActiveItem as MyShowsViewModel;

                if (myShows != null)
                {
                    return myShows.CanRefreshShows;
                }

                return false;
            }
        }

        public void RefreshList()
        {
            var myShows = this.ActiveItem as MyShowsViewModel;

            if (myShows != null)
            {
                myShows.RefreshShows();
            }
        }

    }
}