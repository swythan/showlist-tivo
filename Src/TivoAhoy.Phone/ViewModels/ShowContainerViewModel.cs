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
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;

        private readonly Func<LazyRecordingFolderItemViewModel> showModelFactory;

        private IEnumerable<LazyRecordingFolderItemViewModel> shows;

        public ShowContainerViewModel(
            IProgressService progressService,
            ITivoConnectionService connectionService,
            Func<LazyRecordingFolderItemViewModel> showModelFactory)
        {
            this.connectionService = connectionService;
            this.progressService = progressService;

            this.showModelFactory = showModelFactory;
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
            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                using (progressService.Show())
                {
                    var ids = await connection.GetRecordingFolderItemIds(this.Source != null ? this.Source.Id : null);
                    this.Shows = ids
                        .Select(CreateShowViewModel)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
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
