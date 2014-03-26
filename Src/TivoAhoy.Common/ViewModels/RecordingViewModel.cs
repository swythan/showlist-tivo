//-----------------------------------------------------------------------
// <copyright file="RecordingViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Events;
using TivoAhoy.Common.Services;

namespace TivoAhoy.Common.ViewModels
{
    public class RecordingViewModel : PropertyChangedBase
    {
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;

        string recordingId;

        private Task recordingTask;
        private Recording recording;

        public RecordingViewModel(
            ITivoConnectionService connectionService,
            IProgressService progressService)
        {
            this.connectionService = connectionService;
            this.progressService = progressService;
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

        private async Task UpdateRecordingAsync()
        {
            try
            {
                var connection = this.connectionService.Connection;
                if (connection == null)
                {
                    return;
                }

                using (this.progressService.Show())
                {
                    this.Recording = await connection.GetRecordingDetails(this.recordingId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error fetching recording details for recording {0} : {1}", this.recordingId, ex);
            }
        }
    }
}
