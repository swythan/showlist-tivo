using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using Microsoft;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class ShowContainerViewModel : RecordingFolderItemViewModel<Container>
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ITivoConnectionService connectionService;

        private readonly Func<LazyRecordingFolderItemViewModel> showModelFactory;

        private IEnumerable<LazyRecordingFolderItemViewModel> shows;

        public ShowContainerViewModel(
            IEventAggregator eventAggregator,
            ITivoConnectionService connectionService,
            Func<LazyRecordingFolderItemViewModel> showModelFactory)
        {
            this.connectionService = connectionService;
            this.eventAggregator = eventAggregator;

            this.showModelFactory = showModelFactory;
        }

        private void OnOperationStarted()
        {
            this.eventAggregator.Publish(new TivoOperationStarted());
        }

        private void OnOperationFinished()
        {
            this.eventAggregator.Publish(new TivoOperationFinished());
        }

        public override bool IsSingleShow
        {
            get { return false; }
        }

        public IEnumerable<LazyRecordingFolderItemViewModel> Shows
        {
            get { return shows; }
            set
            {
                if (this.shows == value)
                    return;

                this.shows = value;
                NotifyOfPropertyChange(() => this.Shows);
            }
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

        public async void GetChildShows()
        {
            OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                var ids = await connection.GetRecordingFolderItemIds(this.Source != null ? this.Source.Id : null);
                this.Shows = ids
                    .Select(CreateShowViewModel)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
            }
            finally
            {
                OnOperationFinished();
            }
        }

        private LazyRecordingFolderItemViewModel CreateShowViewModel(long itemId)
        {
            var model = showModelFactory();
            model.Initialise(itemId);

            return model;
        }
    }
}
