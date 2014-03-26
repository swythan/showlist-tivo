//-----------------------------------------------------------------------
// <copyright file="ScheduledRecordingsService.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Events;

namespace TivoAhoy.Common.Services
{
    public class ScheduledRecordingsService : PropertyChangedBase, IScheduledRecordingsService
    {
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;

        private IEnumerable<Recording> recordings;
        private Dictionary<string, Recording> recordingsByOfferId;

        public ScheduledRecordingsService(
            IProgressService progressService,
            ITivoConnectionService connectionService)
        {
            this.progressService = progressService;
            this.connectionService = connectionService;

            connectionService.PropertyChanged += OnConnectionServicePropertyChanged;

            InitializeRecordingsService();
        }

        private async void InitializeRecordingsService()
        {
            if (connectionService.IsConnected)
            {
                await RefreshRecordings();
            }
        }

        public Recording GetScheduledRecordingForOffer(string offerId)
        {
            if (this.recordingsByOfferId == null)
            {
                return null;
            }

            Recording result;
            this.recordingsByOfferId.TryGetValue(offerId, out result);

            return result;
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
            
            var connection = this.connectionService.Connection;
            if (connection == null)
            {
                return;
            }

            using (progressService.Show())
            {
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
        }

        private async void OnConnectionServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
            {
                NotifyOfPropertyChange(() => this.CanRefreshRecordings);

                await this.RefreshRecordings();
            }
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
                this.recordingsByOfferId = this.recordings.ToDictionary(x => x.OfferId);

                this.NotifyOfPropertyChange(() => this.ScheduledRecordings);
            }
        }
    }
}
