using System;
using System.Collections.Generic;
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

        public bool CanUpdate
        {
            get { return this.tivoConnectionService.IsConnected; }
        }

        public async void Update()
        {
            try
            {
                var connection = await this.tivoConnectionService.GetConnectionAsync();

                this.Rows = await connection.GetGridShowsAsync(DateTime.Now, DateTime.Now + TimeSpan.FromMinutes(30), 100, 10, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Request Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
  }
}
