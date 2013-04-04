using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class LazyRecordingFolderItemViewModel : PropertyChangedBase
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ITivoConnectionService connectionService;
        private readonly Func<IndividualShowViewModel> showViewModelFactory;
        private readonly Func<ShowContainerViewModel> showContainerViewModelFactory;

        private long objectId;

        private Task itemTask;
        private IRecordingFolderItemViewModel item;

        public LazyRecordingFolderItemViewModel(
            IEventAggregator eventAggregator,
            ITivoConnectionService connectionService,
            Func<IndividualShowViewModel> showViewModelFactory,
            Func<ShowContainerViewModel> showContainerViewModelFactory)

        {
            this.eventAggregator = eventAggregator;
            this.connectionService = connectionService;
            this.showViewModelFactory = showViewModelFactory;
            this.showContainerViewModelFactory = showContainerViewModelFactory;
        }

        public void Initialise(long objectId)
        {
            this.objectId = objectId;
        }

        public IRecordingFolderItemViewModel Item
        {
            get
            {
                if (this.item != null)
                {
                    return this.item;
                }

                if (this.itemTask == null)
                {
                    this.itemTask = UpdateItemAsync();
                }

                return null;
            }

            private set
            {
                this.item = value;
                this.NotifyOfPropertyChange(() => this.Item);
            }
        }

        private void OnOperationStarted()
        {
            this.eventAggregator.Publish(new TivoOperationStarted());
        }

        private void OnOperationFinished()
        {
            this.eventAggregator.Publish(new TivoOperationFinished());
        }

        private async Task UpdateItemAsync()
        {
            this.OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                var results = await connection.GetRecordingFolderItems(new[] { this.objectId });

                var source = results.FirstOrDefault();

                this.Item = CreateItemViewModel(source);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error fetching recording folder item details for item {0} : {1}", this.objectId, ex);
            }
            finally
            {
                this.OnOperationFinished();
            }
        }

        private IRecordingFolderItemViewModel CreateItemViewModel(RecordingFolderItem recordingFolderItem)
        {
            var showContainer = recordingFolderItem as Container;
            if (showContainer != null)
            {
                var result = this.showContainerViewModelFactory();
                result.Source = showContainer;

                return result;
            }

            var show = recordingFolderItem as IndividualShow;
            if (show != null)
            {
                var result = this.showViewModelFactory();
                result.Source = show;

                return result;
            }

            return null;
        }
    }
}
