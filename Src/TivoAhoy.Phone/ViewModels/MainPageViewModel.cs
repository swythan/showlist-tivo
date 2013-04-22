namespace TivoAhoy.Phone.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using Caliburn.Micro;
    using TivoAhoy.Phone.Events;

    public class MainPageViewModel :
        Conductor<IScreen>.Collection.OneActive,
        IHandle<TivoOperationStarted>,
        IHandle<TivoOperationFinished>
    {
        private readonly INavigationService navigationService;

        private int operationsInProgress = 0;

        public MainPageViewModel(
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            MyShowsViewModel myShowsViewModel,
            ChannelListViewModel channelListViewModel,
            ToDoListViewModel toDoListViewModel)
        {
            this.navigationService = navigationService;

            eventAggregator.Subscribe(this);

            channelListViewModel.DisplayName = "guide";
            channelListViewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.Items.Add(channelListViewModel);

            toDoListViewModel.DisplayName = "scheduled";
            toDoListViewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.Items.Add(toDoListViewModel);

            myShowsViewModel.DisplayName = "my shows";
            myShowsViewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.Items.Add(myShowsViewModel);

            this.ActivateItem(channelListViewModel);
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

        public bool IsOperationInProgress
        {
            get
            {
                return this.operationsInProgress > 0;
            }
        }

        public void Handle(TivoOperationStarted message)
        {
            Interlocked.Increment(ref this.operationsInProgress);
            NotifyOfPropertyChange(() => this.IsOperationInProgress);
        }

        public void Handle(TivoOperationFinished message)
        {
            Interlocked.Decrement(ref this.operationsInProgress);
            NotifyOfPropertyChange(() => this.IsOperationInProgress);
        }
    }
}