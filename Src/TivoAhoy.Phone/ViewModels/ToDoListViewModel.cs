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
    public class ToDoListViewModel : Screen
    {
        private readonly IEventAggregator eventAggregator;
        private readonly INavigationService navigationService;
        private readonly IScheduledRecordingsService scheduledRecordingService;
        private readonly Func<RecordingViewModel> recordingViewModelFactory;

        private IList toDoList;

        public ToDoListViewModel(
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            IScheduledRecordingsService scheduledRecordingService,
            Func<RecordingViewModel> recordingViewModelFactory)
        {
            this.eventAggregator = eventAggregator;
            this.navigationService = navigationService;
            this.scheduledRecordingService = scheduledRecordingService;
            this.recordingViewModelFactory = recordingViewModelFactory;

            scheduledRecordingService.PropertyChanged += OnScheduledRecordingServicePropertyChanged;
        }

        
        public ToDoListViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.ToDoList = new List<RecordingViewModel>()
            {
                RecordingViewModel.CreateDesignTime(
                    new Recording()
                    {
                        Title = "Antiques Roadshow",
                        ScheduledStartTime = DateTime.Parse("2013-05-01T22:59:00"),
                        State = "inProgress",
                        Channel = new Channel()
                        {
                            ChannelNumber = 101,
                            CallSign = "BBC 1",
                            LogoIndex = 65736
                        }
                    }),
                RecordingViewModel.CreateDesignTime(
                    new Recording()
                    {
                        Title = "Charlie Brooker's Weekly Wipe",
                        ScheduledStartTime = DateTime.Parse("2013-05-04T10:29:00"),
                        Channel = new Channel()
                    {
                            ChannelNumber = 102,
                            CallSign = "BBC 2",
                            LogoIndex = 65738
                    }})
            };
        }

        private void OnScheduledRecordingServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CanRefreshRecordings")
            {
                NotifyOfPropertyChange(() => this.CanRefreshToDoList);
            }

            if (e.PropertyName == "ScheduledRecordings")
            {
                if (this.IsActive)
                {
                    this.UpdateToDoList();
                }
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            NotifyOfPropertyChange(() => this.CanRefreshToDoList);

            if (this.ToDoList == null ||
                this.ToDoList.Count == 0)
            {
                Task.Factory.StartNew(() => this.UpdateToDoList());
            }
        }

        public IList ToDoList
        {
            get { return this.toDoList; }
            private set
            {
                this.toDoList = value;
                this.NotifyOfPropertyChange(() => this.ToDoList);
            }
        }

        public bool CanRefreshToDoList
        {
            get
            {
                return this.scheduledRecordingService.CanRefreshRecordings;
            }
        }

        public async void RefreshToDoList()
        {
            if (this.scheduledRecordingService.CanRefreshRecordings)
            {
                try
                {
                    await this.scheduledRecordingService.RefreshRecordings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
                }
            }
        }

        private void UpdateToDoList()
        {
            var recordings = this.scheduledRecordingService.ScheduledRecordings;

            if (recordings != null)
            {
                this.ToDoList = recordings
                    .Select(x => CreateRecordingViewModel(x))
                    .ToList();
            }
        }

        private RecordingViewModel CreateRecordingViewModel(Recording recording)
        {
            var model = this.recordingViewModelFactory();
            model.Initialise(recording.RecordingId);

            return model;
        }

        public void DisplayRecordingDetails(RecordingViewModel recording)
        {
            if (recording == null ||
                recording.Recording == null ||
                recording.Recording.ContentId == null ||
                recording.Recording.RecordingId == null ||
                recording.Recording.OfferId == null)
            {
                return;
            }

            this.navigationService
                .UriFor<ShowDetailsPageViewModel>()
                .WithParam(x => x.ShowContentID, recording.Recording.ContentId)
                .WithParam(x => x.ShowRecordingID, recording.Recording.RecordingId)
                .WithParam(x => x.ShowOfferID, recording.Recording.OfferId)
                .Navigate();
        }
    }
}
