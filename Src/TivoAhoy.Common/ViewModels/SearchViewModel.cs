using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Services;

namespace TivoAhoy.Common.ViewModels
{
    public class SearchViewModel : Screen
    {
        private readonly IProgressService progressService;
        private readonly ISpeechService speechService;
        private readonly ITivoConnectionService connectionService;
        private readonly Func<PersonItemViewModel> personFactory;
        private readonly Func<CollectionItemViewModel> collectionFactory;

        bool isSearchInProgress = false;

        private string searchText;
        private IList<IUnifiedItemViewModel> results;

        public SearchViewModel(
            IProgressService progressService,
            ISpeechService speechService,
            ITivoConnectionService connectionService,
            Func<PersonItemViewModel> personFactory,
            Func<CollectionItemViewModel> collectionFactory
            )
        {
            this.connectionService = connectionService;
            this.progressService = progressService;
            this.speechService = speechService;

            this.personFactory = personFactory;
            this.collectionFactory = collectionFactory;

            connectionService.PropertyChanged += OnConnectionServicePropertyChanged;
        }

        private void OnConnectionServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
            {
                NotifyOfPropertyChange(() => this.CanSearch);
                NotifyOfPropertyChange(() => this.CanSearchByVoice);
            }
        }

        public bool IsSearchInProgress
        {
            get
            {
                return this.isSearchInProgress;
            }

            set
            {
                this.isSearchInProgress = value;
                
                NotifyOfPropertyChange(() => this.IsSearchInProgress);
                NotifyOfPropertyChange(() => this.CanSearch);
                NotifyOfPropertyChange(() => this.CanSearchByVoice);
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
            if (!this.CanSearch)
            {
                return;
            }

            this.IsSearchInProgress = true;

            try
            {
                using (this.progressService.Show())
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Search Failed\n{0}", ex.Message));
            }
            finally
            {
                this.IsSearchInProgress = false;
            }
        }

        public bool CanSearchByVoice
        {
            get
            {
                return this.connectionService.IsConnected && this.speechService != null;
            }
        }

        public async void SearchByVoice()
        {
            string findText = await this.speechService.RecognizeTextFromWebSearchGrammar("Ex. \"The Simpsons\"");

            if (string.IsNullOrWhiteSpace(findText))
            {
                return;
            }

            this.SearchText = findText;

            if (this.CanSearchByVoice)
            {
                this.Search();

                await this.speechService.Speak(string.Format("Searching for {0}", this.SearchText));
            }
            else
            { 
                await this.speechService.Speak("Sorry, search not available.");
            }
        }

        private IUnifiedItemViewModel CreateItemViewModel(IUnifiedItem item)
        {
            var person = item as Person;
            if (person != null)
            {
                var viewModel = personFactory();
                viewModel.Source = person;

                return viewModel;
            }

            var collection = item as Collection;
            if (collection != null)
            {
                var viewModel = collectionFactory();
                viewModel.Source = collection;
                return viewModel;
            }

            return null;
        }
    }
}
