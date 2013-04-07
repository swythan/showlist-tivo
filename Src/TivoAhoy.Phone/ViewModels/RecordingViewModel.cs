using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class RecordingViewModel : PropertyChangedBase
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ITivoConnectionService connectionService;

        string recordingId;

        private Task recordingTask;
        private Recording recording;

        public RecordingViewModel(
            ITivoConnectionService connectionService,
            IEventAggregator eventAggregator)
        {
            this.connectionService = connectionService;
            this.eventAggregator = eventAggregator;
        }

        public void Initialise(string recordingId)
        {
            this.recordingId = recordingId;
        }

        public static RecordingViewModel CreateDesignTime(Recording recording)
        {
            var model = new RecordingViewModel(null, null);

            model.recording = recording;

            return model;
        }

        public Channel Channel
        {
            get
            {
                if (this.recording == null)
                    return null;

                return this.recording.Channel;
            }
        }

        public Recording Recording
        {
            get
            {
                if (recording != null)
                {
                    return this.recording;
                }

                if (this.recording == null)
                {
                    this.recordingTask = UpdateRecordingAsync();
                }

                return null;
            }

            private set
            {
                this.recording = value;
                this.NotifyOfPropertyChange(() => this.Recording);
                this.NotifyOfPropertyChange(() => this.Channel);
                this.NotifyOfPropertyChange(() => this.IsInProgress);
            }
        }
        
        public bool IsInProgress
        {
            get
            {
                if (this.recording == null)
                {
                    return false;
                }

                return this.recording.State == "inProgress";
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

        private async Task UpdateRecordingAsync()
        {
            this.OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                this.Recording = await connection.GetRecordingDetails(this.recordingId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error fetching recording details for recording {0} : {1}", this.recordingId, ex);
            }
            finally
            {
                this.OnOperationFinished();
            }
        }
    }
}
