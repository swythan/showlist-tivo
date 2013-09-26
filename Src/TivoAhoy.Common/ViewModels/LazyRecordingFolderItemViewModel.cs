//-----------------------------------------------------------------------
// <copyright file="LazyRecordingFolderItemViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Events;
using TivoAhoy.Common.Services;

namespace TivoAhoy.Common.ViewModels
{
    public class LazyRecordingFolderItemViewModel : PropertyChangedBase
    {
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;
        private readonly Func<IndividualShowViewModel> showViewModelFactory;
        private readonly Func<ShowContainerViewModel> showContainerViewModelFactory;

        private long objectId;

        private Task itemTask;
        private IRecordingFolderItemViewModel item;

        public LazyRecordingFolderItemViewModel(
            IProgressService progressService,
            ITivoConnectionService connectionService,
            Func<IndividualShowViewModel> showViewModelFactory,
            Func<ShowContainerViewModel> showContainerViewModelFactory)

        {
            this.progressService = progressService;
            this.connectionService = connectionService;
            this.showViewModelFactory = showViewModelFactory;
            this.showContainerViewModelFactory = showContainerViewModelFactory;
        }

        /// <summary>
        /// Design mode constructor
        /// </summary>
        public LazyRecordingFolderItemViewModel(RecordingFolderItem item)
        {
            this.showViewModelFactory = () => new IndividualShowViewModel();
            this.showContainerViewModelFactory = () => new ShowContainerViewModel();

            this.Item = CreateItemViewModel(item);
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

        private async Task UpdateItemAsync()
        {
            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                using (progressService.Show())
                {
                    var results = await connection.GetRecordingFolderItems(new[] { this.objectId });

                    var source = results.FirstOrDefault();

                    this.Item = CreateItemViewModel(source);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error fetching recording folder item details for item {0} : {1}", this.objectId, ex);
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
