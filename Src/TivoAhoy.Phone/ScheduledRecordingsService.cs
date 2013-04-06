using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone
{
    public class ScheduledRecordingsService : PropertyChangedBase, IScheduledRecordingsService
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ITivoConnectionService connectionService;

        private IEnumerable<Recording> recordings;
        private Dictionary<string, Recording> recordingsByOfferId;

        public ScheduledRecordingsService(
            IEventAggregator eventAggregator,
            ITivoConnectionService connectionService)
        {
            this.eventAggregator = eventAggregator;
            this.connectionService = connectionService;

            connectionService.PropertyChanged += OnConnectionServicePropertyChanged;

            if (connectionService.IsConnected)
            {
                RefreshRecordings();
            }
        }

        public bool IsOfferRecordingScheduled(string offerId)
        {
            if (this.recordingsByOfferId == null)
            {
                return false;
            }

            return this.recordingsByOfferId.ContainsKey(offerId);
        }

        public bool CanRefreshRecordings
        {
            get
            {
                return this.connectionService.IsConnected;
            }
        }

        public async Task RefreshRecordings()
        {
            if (!this.CanRefreshRecordings)
            {
                return;
            }

            OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                var recordingIds = await connection.GetScheduledRecordingIds();

                var recordings = new List<Recording>();

                int pageSize = 20;
                for (int offset = 0; offset < recordingIds.Count; offset += pageSize)
                {
                    var page = await connection.GetScheduledRecordings(offset, pageSize);
                    recordings.AddRange(page);
                }

                this.ScheduledRecordings = recordings;
            }
            finally
            {
                OnOperationFinished();
            }
        }

        private void OnConnectionServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
            {
                NotifyOfPropertyChange(() => this.CanRefreshRecordings);

                this.RefreshRecordings();
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
        
        public IEnumerable<Recording> ScheduledRecordings 
        { 
            get
            {
                return this.recordings;
            }

            set
            {
                this.recordings = value;
                this.recordingsByOfferId = this.recordings.ToDictionary(x => x.offerId);

                this.NotifyOfPropertyChange(() => this.ScheduledRecordings);
            }
        }
    }
}
