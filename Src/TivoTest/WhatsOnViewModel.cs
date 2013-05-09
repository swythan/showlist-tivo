using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;

namespace TivoTest
{
    [Export(typeof(WhatsOnViewModel))]
    public class WhatsOnViewModel : PropertyChangedBase
    {
        private readonly ITivoConnectionService tivoConnectionService;

        private ShowDetails showDetails;
        private bool isOperationInProgress;

        [ImportingConstructor]
        public WhatsOnViewModel(ITivoConnectionService tivoConnectionService)
        {
            this.tivoConnectionService = tivoConnectionService;
            this.tivoConnectionService.PropertyChanged += (sender, args) => this.NotifyOfPropertyChange(() => this.CanUpdate);
        }

        public string ShowContentID { get; set; }

        public bool CanUpdate
        {
            get
            {
                return this.tivoConnectionService.IsConnected && !this.tivoConnectionService.IsAwayModeEnabled;
            }
        }
        
        public async Task Update()
        {

            OnOperationStarted();

            try
            {
                this.Show = null;

                var connection = await this.tivoConnectionService.GetConnectionAsync();

                this.Show = await connection.GetWhatsOn();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Request Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                OnOperationFinished();
            }
        }

        public bool IsOperationInProgress
        {
            get { return this.isOperationInProgress; }
        }

        private void OnOperationStarted()
        {
            this.isOperationInProgress = true;
            NotifyOfPropertyChange(() => this.IsOperationInProgress);
        }

        private void OnOperationFinished()
        {
            this.isOperationInProgress = false;
            NotifyOfPropertyChange(() => this.IsOperationInProgress);
        }

        public bool HasEpisodeNumbers
        {
            get
            {
                return
                    this.Show != null &&
                    this.Show.EpisodeNumber != null &&
                    this.Show.SeasonNumber != null;
            }
        }

        public bool HasOriginalAirDate
        {
            get
            {
                return
                    this.Show != null &&
                    this.Show.OriginalAirDate != default(DateTime);
            }
        }

        public Uri MainImage
        {
            get
            {
                if (this.Show == null ||
                    this.Show.Images == null)
                {
                    return null;
                }

                var widthCutoff = 400;

                var imagesWithWidth = this.Show.Images.Where(x => x.Width != null);
                var largeImages = imagesWithWidth.Where(x => x.Width >= widthCutoff).OrderBy(x => x.Width);
                var smallImages = imagesWithWidth.Where(x => x.Width < widthCutoff).OrderByDescending(x => x.Width);

                var bestImage = largeImages.Concat(smallImages).Concat(this.Show.Images.Except(imagesWithWidth)).FirstOrDefault();

                if (bestImage == null)
                {
                    return null;
                }

                return bestImage.ImageUrl;
            }
        }

        public ShowDetails Show
        {
            get { return this.showDetails; }
            set
            {
                this.showDetails = value;

                //if (value != null)
                //{
                //    var json = value.JsonText;
                //    Debug.WriteLine("Show details fetched:");

                //    foreach (var line in json.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
                //    {
                //        Debug.WriteLine(line);
                //    }

                //    Debug.WriteLine(string.Empty);
                //}

                NotifyOfPropertyChange(() => this.Show);
                NotifyOfPropertyChange(() => this.MainImage);
                NotifyOfPropertyChange(() => this.HasEpisodeNumbers);
                NotifyOfPropertyChange(() => this.HasOriginalAirDate);
            }
        }

    }
}
