using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoTest
{
    [Export(typeof(ShowGridViewModel))]
    public class ShowGridViewModel : PropertyChangedBase
    {
        private readonly ITivoConnectionService tivoConnectionService;
        
        private IEnumerable<GridRow> rows;
        private DateTime startTime;
        private DateTime endTime;

        [ImportingConstructor]
        public ShowGridViewModel(ITivoConnectionService tivoConnectionService)
        {
            this.tivoConnectionService = tivoConnectionService;
            this.tivoConnectionService.PropertyChanged += OnConnectionServicePropertyChanged;
        }

        private void OnConnectionServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyOfPropertyChange(() => this.CanUpdate);
        }

        public IEnumerable<GridRow> Rows
        {
            get
            {
                return this.rows;
            }

            private set
            {
                this.rows = value;
                NotifyOfPropertyChange(() => this.Rows);
            }
        }

        public DateTime StartTime
        {
            get
            {
                return this.startTime;
            }

            set
            {
                if (this.startTime == value)
                {
                    return;
                }

                this.startTime = value;
                NotifyOfPropertyChange(() => this.StartTime);
            }
        }

        public DateTime EndTime
        {
            get
            {
                return this.endTime;
            }

            set
            {
                if (this.endTime == value)
                {
                    return;
                }

                this.endTime = value;
                NotifyOfPropertyChange(() => this.EndTime);
            }
        }

        public bool CanUpdate
        {
            get { return this.tivoConnectionService.IsConnected; }
        }

        public async void Update()
        {
            try
            {
                var connection = await this.tivoConnectionService.GetConnectionAsync();

                var now = DateTime.Now;

                var startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, (now.Minute / 15) * 15, 00);

                this.StartTime = startTime;
                this.EndTime = this.StartTime + TimeSpan.FromMinutes(15);

                var newRows = new ObservableCollection<GridRow>();

                this.Rows = newRows;

                for (int i = 0; i < 10; i++)
                {
                    int pageSize = 5;

                    var extraRows = await connection.GetGridShowsAsync(this.StartTime, this.EndTime, 100, pageSize, pageSize * i);

                    foreach (var row in extraRows)
                    {
                        newRows.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Request Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
  }
}
