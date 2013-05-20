using System.ComponentModel;
using Caliburn.Micro;

namespace TivoAhoy.Common.ViewModels
{
    public class ShowContainerShowsPageViewModel : Screen
    {
        private string title;

        public ShowContainerShowsPageViewModel(MyShowsViewModel myShowsViewModel)
        {
            this.Content = myShowsViewModel;
            myShowsViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public MyShowsViewModel Content { get; set; }

        public string ParentId
        {
            get { return this.Content.ParentId; }
            set
            {
                this.Content.ParentId = value;
            }
        }

        public string Title
        {
            get { return this.title; }
            set
            {
                this.title = value;
                this.NotifyOfPropertyChange(() => this.Title);
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            NotifyOfPropertyChange(() => this.CanRefreshList);

            if (this.CanRefreshList)
            {
                this.RefreshList();
            }
        }

        public bool CanRefreshList
        {
            get
            {
                return this.Content.CanRefreshShows;
            }
        }

        public void RefreshList()
        {
            this.Content.RefreshShows();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "CanRefreshShows")
            {
                this.NotifyOfPropertyChange(() => this.CanRefreshList);
            }
        }

    }
}
