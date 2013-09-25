//-----------------------------------------------------------------------
// <copyright file="SearchViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using Coding4Fun.Toolkit.Controls;
using Tivo.Connect;
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

        private Subject<Unit> searchTextChangedSubject = new Subject<Unit>();
        private string lastSearchText = null;

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

            this.connectionService.PropertyChanged += OnConnectionServicePropertyChanged;

            this.searchTextChangedSubject
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                    {
                        if (this.SearchText != this.lastSearchText)
                        {
                            this.Search();
                        }
                    });
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

                this.searchTextChangedSubject.OnNext(Unit.Default);
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

                    this.lastSearchText = this.SearchText;

                    IEnumerable<IUnifiedItem> result = await connection.ExecuteUnifiedItemSearch(this.SearchText, 0, 25);

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
                Execute.BeginOnUIThread(() =>
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Search failed",
                        Message = ex.Message,
                        TextOrientation = Orientation.Vertical,
                        TextWrapping = TextWrapping.Wrap,
                        Background = new SolidColorBrush(Colors.Red),
                    };

                    toast.Show();
                });
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
            // Start connecting now (in case we are disconnected, esp at start-up)
            var connectTask = this.connectionService.GetConnectionAsync();

            string findText = await this.speechService.RecognizeTextFromWebSearchGrammar("Ex. \"The Simpsons\"");

            if (string.IsNullOrWhiteSpace(findText))
            {
                return;
            }

            this.SearchText = findText;

            // Wait for the connection to succeed (or fail)
            await connectTask;

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
