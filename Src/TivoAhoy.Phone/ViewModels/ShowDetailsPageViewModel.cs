using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;

namespace TivoAhoy.Phone.ViewModels
{
    public class ShowDetailsPageViewModel : Screen
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;

        private ShowDetails showDetails;
        private Recording recordingDetails;

        private bool isOperationInProgress;
        private int panoramaHeight = 800;

        public ShowDetailsPageViewModel(
            IEventAggregator eventAggregator,
            IProgressService progressService,
            ITivoConnectionService connectionService)
        {
            this.eventAggregator = eventAggregator;
            this.progressService = progressService;
            this.connectionService = connectionService;
        }

        public ShowDetailsPageViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.ShowContentID = "fakeContent";
            this.ShowRecordingID = "fakeRecording";

            this.Show = new ShowDetails()
            {
                Title = "The Walking Dead",
                Subtitle = "An Interesting Episode",
                SeasonNumber = 2,
                EpisodeNumbers = new List<int>() { 3 },
                OriginalAirDate = new DateTime(2012, 11, 16),
                Images = new List<ImageInfo>
                {
                    new ImageInfo()
                    {
                        Height= 270,
                        Width = 360,
                        OriginalImageUrl= "http://10.185.116.1:8080/images/os/banner-270/55/12/551290d0c77e87f7dbb1ca3db42e3b3f.jpg"
                    }
                },
                Description = "This is the description of this very interesting episode. Don't let it catch you out, as it really is very interesting"

            };
        }

        public int PanoramaHeight
        {
            get
            {
                return this.panoramaHeight;
            }
            set
            {
                if (this.panoramaHeight == value)
                {
                    return;
                }

                this.panoramaHeight = value;
                NotifyOfPropertyChange(() => this.PanoramaHeight);
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
            NotifyOfPropertyChange(() => this.CanPlayShow);
            NotifyOfPropertyChange(() => this.CanDeleteShow);
            NotifyOfPropertyChange(() => this.CanCancelRecording);
            NotifyOfPropertyChange(() => this.CanScheduleRecording);
        }

        private void OnOperationFinished()
        {
            this.isOperationInProgress = false;
            NotifyOfPropertyChange(() => this.IsOperationInProgress);
            NotifyOfPropertyChange(() => this.CanPlayShow);
            NotifyOfPropertyChange(() => this.CanDeleteShow);
            NotifyOfPropertyChange(() => this.CanCancelRecording);
            NotifyOfPropertyChange(() => this.CanScheduleRecording);
        }

        public string ShowContentID { get; set; }
        public string ShowRecordingID { get; set; }
        public string ShowOfferID { get; set; }

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

                var widthCutoff = 1024;

                var bestImage = GetBestImageForWidth(widthCutoff);

                if (bestImage == null)
                {
                    return null;
                }

                return bestImage.ImageUrl;
            }
        }

        private ImageInfo GetBestImageForWidth(int widthCutoff)
        {
            if (this.Show == null ||
                this.Show.Images == null)
            {
                return null;
            }

            var imagesWithWidth = this.Show.Images.Where(x => x.Width != null).ToList();
            var largeImages = imagesWithWidth.Where(x => x.Width >= widthCutoff).OrderBy(x => x.Width).ToList();
            var smallImages = imagesWithWidth.Where(x => x.Width < widthCutoff).OrderByDescending(x => x.Width).ToList();

            var bestImage = largeImages.Concat(smallImages).Concat(this.Show.Images.Except(imagesWithWidth)).FirstOrDefault();
            return bestImage;
        }

        private ImageInfo GetBestImageForHeight(int heightCutoff)
        {
            if (this.Show == null ||
                this.Show.Images == null)
            {
                return null;
            }

            var imagesWithHeight = this.Show.Images.Where(x => x.Height != null).ToList();
            var largeImages = imagesWithHeight.Where(x => x.Height >= heightCutoff).OrderBy(x => x.Height).ToList();
            var smallImages = imagesWithHeight.Where(x => x.Height < heightCutoff).OrderByDescending(x => x.Height).ToList();

            var bestImage = largeImages.Concat(smallImages).Concat(this.Show.Images.Except(imagesWithHeight)).FirstOrDefault();
            return bestImage;
        }

        private async void UpdateBackgroundBrush()
        {
            this.MainImageBrush = null;

            if (Execute.InDesignMode)
            {
                return;
            }

            var bestImage = this.GetBestImageForHeight(this.PanoramaHeight);

            if (bestImage != null)
            {
                var bi = new BitmapImage();
                if (await bi.SetUriSourceAsync(bestImage.ImageUrl))
                {
                    var wb = new WriteableBitmap(bi);
                    bi.UriSource = null;

                    var bigImage = new BitmapImage();
                    using (var tempStream = new MemoryStream())
                    {
                        var aspectRatio = wb.PixelHeight / (double)wb.PixelWidth;

                        wb.SaveJpeg(tempStream, (int)(this.PanoramaHeight / aspectRatio), this.PanoramaHeight, 0, 95);

                        tempStream.Seek(0, SeekOrigin.Begin);
                        if (await bigImage.SetSourceAsync(tempStream))
                        {
                            ImageBrush brush = new ImageBrush();
                            brush.ImageSource = bigImage;
                            brush.Stretch = Stretch.Uniform;
                            brush.Opacity = 0.4;

                            this.MainImageBrush = brush;
                        }
                    }
                }
            }

            this.NotifyOfPropertyChange(() => MainImageBrush);
        }

        public ImageBrush MainImageBrush
        {
            get;
            set;
        }

        public ShowDetails Show
        {
            get { return this.showDetails; }
            set
            {
                this.showDetails = value;

                Debug.WriteLine("Show details fetched:");

                NotifyOfPropertyChange(() => this.Show);
                //NotifyOfPropertyChange(() => this.IsRecorded);
                NotifyOfPropertyChange(() => this.MainImage);
                NotifyOfPropertyChange(() => this.MainImageBrush);
                NotifyOfPropertyChange(() => this.HasEpisodeNumbers);
                NotifyOfPropertyChange(() => this.HasOriginalAirDate);
                //NotifyOfPropertyChange(() => this.CanDeleteShow);
                //NotifyOfPropertyChange(() => this.CanPlayShow);

                UpdateBackgroundBrush();
            }
        }

        public Recording Recording
        {
            get { return this.recordingDetails; }
            set
            {
                this.recordingDetails = value;

                Debug.WriteLine("Recording details fetched:");

                NotifyOfPropertyChange(() => this.Recording);
                NotifyOfPropertyChange(() => this.IsRecorded);
                NotifyOfPropertyChange(() => this.IsRecordable);
                NotifyOfPropertyChange(() => this.IsScheduled);
                NotifyOfPropertyChange(() => this.CanDeleteShow);
                NotifyOfPropertyChange(() => this.CanPlayShow);
                NotifyOfPropertyChange(() => this.CanCancelRecording);
                NotifyOfPropertyChange(() => this.CanScheduleRecording);

                UpdateBackgroundBrush();
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            FetchShowDetails();
        }

        private async void FetchShowDetails()
        {

            OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                using (progressService.Show())
                {
                    if (!string.IsNullOrEmpty(this.ShowRecordingID))
                    {
                        this.Recording = await connection.GetRecordingDetails(this.ShowRecordingID);
                    }

                    this.Show = await connection.GetShowContentDetails(this.ShowContentID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to retrieve details:\n{0}", ex.Message));
            }
            finally
            {
                OnOperationFinished();
            }
        }

        public bool IsRecorded
        {
            get
            {
                if (this.Recording == null)
                    return false;

                if (this.Recording.State != "complete" &&
                    this.Recording.State != "inProgress")
                    return false;

                return true;
            }
        }

        public bool IsScheduled
        {
            get
            {
                if (this.Recording == null)
                    return false;

                return
                    this.Recording.State == "scheduled";
            }
        }

        public bool IsRecordable
        {
            get
            {
                if (this.ShowOfferID == null)
                    return false;

                if (this.ShowRecordingID == null)
                    return true;

                if (this.Recording == null)
                    return false;

                // TODO : Should we be able to record existing recordings in certain states (e.g. cancelled)?
                return false;
            }
        }

        public bool CanPlayShow
        {
            get
            {
                if (!this.IsRecorded)
                    return false;

                return
                    !this.connectionService.IsAwayMode &&
                    !this.IsOperationInProgress;
            }
        }

        public async void PlayShow()
        {
            OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                using (progressService.Show())
                {
                    await connection.PlayShow(this.ShowRecordingID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Play command failed:\n{0}", ex.Message));
            }
            finally
            {
                OnOperationFinished();
            }
        }

        public bool CanDeleteShow
        {
            get
            {
                if (this.IsOperationInProgress)
                    return false;

                if (this.Recording == null)
                    return false;

                return this.Recording.State == "complete";
            }
        }

        public async void DeleteShow()
        {
            OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                using (progressService.Show())
                {
                    await connection.DeleteRecording(this.ShowRecordingID);
                }

                MessageBox.Show("Recording deleted.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Delete command failed:\n{0}", ex.Message));
            }
            finally
            {
                OnOperationFinished();
            }
        }

        public bool CanCancelRecording
        {
            get
            {
                if (this.IsOperationInProgress)
                    return false;

                if (!this.IsScheduled)
                    return false;

                return true;
            }
        }

        public async void CancelRecording()
        {
            OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();
                using (progressService.Show())
                {
                    await connection.CancelRecording(this.ShowRecordingID);
                }

                MessageBox.Show("Recording cancelled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Cancel recording command failed:\n{0}", ex.Message));
            }
            finally
            {
                OnOperationFinished();
            }
        }

        public bool CanScheduleRecording
        {
            get
            {
                if (!this.IsRecordable)
                    return false;

                return
                    !this.connectionService.IsAwayMode &&
                    !this.IsOperationInProgress;
            }
        }

        public async void ScheduleRecording()
        {
            OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                SubscribeResult result;
                using (progressService.Show())
                {
                    result = await connection.ScheduleSingleRecording(this.ShowContentID, this.ShowOfferID);
                }

                if (result.Subscription != null)
                {
                    MessageBox.Show("Recording scheduled.");
                }
                else
                {
                    MessageBox.Show(string.Format("Unable to schedule recording due to conflicts."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to schedule recording:\n{0}", ex.Message));
            }
            finally
            {
                OnOperationFinished();
            }
        }
    }
}
