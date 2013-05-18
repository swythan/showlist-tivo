using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class SearchViewModel : Screen
    {
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;
        private string searchText;
        private IList<IUnifiedItemViewModel> results;

        public SearchViewModel(
            IProgressService progressService,
            ITivoConnectionService connectionService
            )
        {
            this.connectionService = connectionService;
            this.progressService = progressService;

            connectionService.PropertyChanged += OnConnectionServicePropertyChanged;
        }

        private void OnConnectionServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
            {
                NotifyOfPropertyChange(() => this.CanSearch);
            }
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

        public IList<IUnifiedItemViewModel> Results
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
            get { return this.connectionService.IsConnected && this.SearchText != null; }
        }

        public async void Search()
        {
            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                IEnumerable<IUnifiedItem> result = await connection.ExecuteUnifiedItemSearch(this.SearchText, 0, 50);

                if (result == null)
                {
                    result = Enumerable.Empty<IUnifiedItem>();
                }

                this.Results = result
                    .Select(x => CreateItemViewModel(x))
                    .Where(x => x != null)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Search Failed\n{0}", ex.Message));
            }
        }

        private IUnifiedItemViewModel CreateItemViewModel(IUnifiedItem item)
        {
            var person = item as Person;
            if (person != null)
            {
                return new PersonItemViewModel(person);
            }

            var collection = item as Collection;
            if (collection != null)
            {
                return new CollectionItemViewModel(collection);
            }

            return null;
        }
    }
}
