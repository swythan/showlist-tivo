using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoTest
{
    [Export(typeof(SearchViewModel))]
    public class SearchViewModel : PropertyChangedBase
    {
        private readonly ITivoConnectionService tivoConnectionService;

        private string searchText;
        private IList<IUnifiedItem> results;

        [ImportingConstructor]
        public SearchViewModel(ITivoConnectionService tivoConnectionService)
        {
            this.tivoConnectionService = tivoConnectionService;
            this.tivoConnectionService.PropertyChanged += OnConnectionServicePropertyChanged;

            this.SearchText = "test";
        }

        private void OnConnectionServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyOfPropertyChange(() => this.CanSearch);
        }

        public string SearchText
        {
            get
            {
                return this.searchText;
            }

            set
            {
                this.searchText = value;
                NotifyOfPropertyChange(() => this.SearchText);
                NotifyOfPropertyChange(() => this.CanSearch);
            }
        }

        public IList<IUnifiedItem> Results
        {
            get
            {
                return this.results;
            }

            private set
            {
                this.results = value;
                NotifyOfPropertyChange(() => this.Results);
            }
        }

        public bool CanSearch
        {
            get { return this.tivoConnectionService.IsConnected && this.SearchText != null; }
        }

        public async void Search()
        {
            try
            {
                var connection = await this.tivoConnectionService.GetConnectionAsync();

                var result = await connection.ExecuteUnifiedItemSearch(this.SearchText, 0, 10);

                this.Results = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Request Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
