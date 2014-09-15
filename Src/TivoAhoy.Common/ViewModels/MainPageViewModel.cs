//-----------------------------------------------------------------------
// <copyright file="MainPageViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace TivoAhoy.Common.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using Caliburn.Micro;
    using TivoAhoy.Common.Events;
    using TivoAhoy.Common.Services;

    public class MainPageViewModel :
        Conductor<IScreen>.Collection.OneActive
    {
        private readonly INavigationService navigationService;
        private readonly ISpeechService speechService;
        private readonly ITivoConnectionService connectionService;

        private readonly MyShowsViewModel myShowsViewModel;
        private readonly ChannelListViewModel channelListViewModel;
        private readonly ToDoListViewModel toDoListViewModel;
        private readonly SearchViewModel searchViewModel;

        private bool isFirstActivation = true;

        public MainPageViewModel(
            INavigationService navigationService,
            ISpeechService speechService,
            ITivoConnectionService connectionService,
            MyShowsViewModel myShowsViewModel,
            ChannelListViewModel channelListViewModel,
            ToDoListViewModel toDoListViewModel,
            SearchViewModel searchViewModel)
        {
            this.navigationService = navigationService;
            this.speechService = speechService;
            this.connectionService = connectionService;

            this.myShowsViewModel = myShowsViewModel;
            this.channelListViewModel = channelListViewModel;
            this.toDoListViewModel = toDoListViewModel;
            this.searchViewModel = searchViewModel;

            connectionService.PropertyChanged += OnConnectionServicePropertyChanged;

            this.channelListViewModel.DisplayName = "guide";
            this.channelListViewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.Items.Add(channelListViewModel);

            this.toDoListViewModel.DisplayName = "scheduled";
            this.toDoListViewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.Items.Add(toDoListViewModel);

            this.myShowsViewModel.DisplayName = "my shows";
            this.myShowsViewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.Items.Add(myShowsViewModel);

            this.searchViewModel.DisplayName = "search";
            this.searchViewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.Items.Add(searchViewModel);

            this.ActivateItem(this.channelListViewModel);
        }

        private void OnConnectionServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SettingsAppearValid")
            {
                NotifyOfPropertyChange(() => this.ShowSettingsPrompt);
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "CanRefreshShows" ||
                args.PropertyName == "CanRefreshToDoList")
            {
                if (sender == this.ActiveItem)
                {
                    this.NotifyOfPropertyChange(() => this.CanRefreshList);
                }
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            NotifyOfPropertyChange(() => this.CanRefreshList);
            NotifyOfPropertyChange(() => this.ShowSettingsPrompt);

            if (this.isFirstActivation)
            {
                this.isFirstActivation = false;

                if (!string.IsNullOrEmpty(this.VoiceCommandName))
                {
                    if (this.VoiceCommandName.Equals("MyShowsList"))
                    {
                        this.ActivateItem(this.myShowsViewModel);
                    }

                    if (this.VoiceCommandName.Equals("ToDoList"))
                    {
                        this.ActivateItem(this.toDoListViewModel);
                    }

                    if (this.VoiceCommandName.Equals("ShowGuide"))
                    {
                        this.ActivateItem(this.channelListViewModel);
                    }

                    if (this.VoiceCommandName.Equals("Search"))
                    {
                        SearchViewModel searchViewModel = this.Items.OfType<SearchViewModel>().First();

                        this.ActivateItem(searchViewModel);
                        searchViewModel.SearchByVoice();
                    }
                }
                else
                {
                    if (this.speechService != null)
                    {
                        this.speechService.EnsureInitVoiceCommandsOnBackgroundThread();
                    }
                }
            }
        }

        protected override void OnActivationProcessed(IScreen item, bool success)
        {
            base.OnActivationProcessed(item, success);
            NotifyOfPropertyChange(() => this.CanRefreshList);
        }

        public string VoiceCommandName
        {
            get;
            set;
        }

        public bool ShowSettingsPrompt
        {
            get
            {
                return !this.connectionService.SettingsAppearValid;
            }
        }

        public void ShowSettings()
        {
            var settingsUri = navigationService.UriFor<SettingsPageViewModel>().BuildUri();

            navigationService.Navigate(settingsUri);
        }

        public void ShowSignInPage()
        {
            var settingsUri = navigationService.UriFor<SignInPageViewModel>().BuildUri();

            navigationService.Navigate(settingsUri);
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

                var channels = this.ActiveItem as ChannelListViewModel;
                if (channels != null)
                {
                    return channels.CanRefreshShows;
                }

                var toDoList = this.ActiveItem as ToDoListViewModel;
                if (toDoList != null)
                {
                    return toDoList.CanRefreshToDoList;
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

            var channels = this.ActiveItem as ChannelListViewModel;
            if (channels != null)
            {
                channels.RefreshShows();
            }

            var toDoList = this.ActiveItem as ToDoListViewModel;
            if (toDoList != null)
            {
                toDoList.RefreshToDoList();
            }
        }

        public void ShowAbout()
        {
            this.navigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
        }
    }
}
