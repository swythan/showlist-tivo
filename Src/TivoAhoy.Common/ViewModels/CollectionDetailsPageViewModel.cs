﻿//-----------------------------------------------------------------------
// <copyright file="CollectionDetailsPageViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using Coding4Fun.Toolkit.Controls;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Services;
using TivoAhoy.Common.Utils;

namespace TivoAhoy.Common.ViewModels
{
    public class CollectionDetailsPageViewModel : Screen
    {
        private readonly IAnalyticsService analyticsService;
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;
        private readonly CreditsViewModel creditsViewModel;
        private readonly UpcomingOffersViewModel upcomingOffersViewModel;

        private Collection collectionDetails;

        private bool isOperationInProgress;
        private int panoramaHeight = 800;

        private ImageBrush mainImageBrush = null;

        public CollectionDetailsPageViewModel(
            IAnalyticsService analyticsService,
            IProgressService progressService,
            ITivoConnectionService connectionService,
            CreditsViewModel creditsViewModel,
            UpcomingOffersViewModel upcomingOffersViewModel)
        {
            this.analyticsService = analyticsService;
            this.progressService = progressService;
            this.connectionService = connectionService;
            this.creditsViewModel = creditsViewModel;
            this.upcomingOffersViewModel = upcomingOffersViewModel;
        }

        public CollectionDetailsPageViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.CollectionID = "fakeContent";

            this.Collection = new Collection()
            {
                Title = "CSI: Crime Scene Investigation",
                Images = new List<ImageInfo>
                {
                    new ImageInfo()
                    {
                        Height= 270,
                        Width = 360,
                        OriginalImageUrl= "http://10.185.116.1:8080/images/os/banner-270/b9/4b/b94b1843d277920b702edef44a8ce272.jpg"
                    }
                },
                Description = "A specialised team of forensic investigators finds the missing pieces to dangerous puzzles.",
                Credits = new List<Credit>
                {
                    new Credit
                    {
                        CharacterName= "D.B. Russell",
                        FirstName ="Ted",
                        LastName= "Danson",
                        PersonId= "tivo:pn.61724",
                        OriginalRole = "actor",
                        Images = new List<ImageInfo>
                        {
                            new ImageInfo
                            {
                                Width= 59,
                                Height = 78,
                                ImageId = "tivo:im.458355819",
                                ImageType= "person",
                                OriginalImageUrl= "http://10.185.116.1:8080/images/legacy/small/69/55/695556e67a7b88608fc01a5c72501a24.jpg",
                            },
                        }
                    }
                }
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
        }

        private void OnOperationFinished()
        {
            this.isOperationInProgress = false;
            NotifyOfPropertyChange(() => this.IsOperationInProgress);
        }

        public string CollectionID { get; set; }

        private async void UpdateBackgroundBrush()
        {
            this.MainImageBrush = null;

            if (Execute.InDesignMode)
            {
                return;
            }

            if (this.Collection == null)
            {
                return;
            }

            int requiredHeight = this.PanoramaHeight;

            var bestImage = this.Collection.Images.GetBestImageForHeight(requiredHeight);

            this.MainImageBrush = await bestImage.GetResizedImageBrushAsync(requiredHeight);
        }


        public ImageBrush MainImageBrush
        {
            get { return this.mainImageBrush; }
            set
            {
                if (this.mainImageBrush == value)
                {
                    return;
                }

                this.mainImageBrush = value;
                this.NotifyOfPropertyChange(() => MainImageBrush);
            }
        }

        public Collection Collection
        {
            get { return this.collectionDetails; }
            set
            {
                this.collectionDetails = value;

                Debug.WriteLine("Collection details fetched:");

                NotifyOfPropertyChange(() => this.Collection);
                NotifyOfPropertyChange(() => this.Credits);
                NotifyOfPropertyChange(() => this.MainImageBrush);

                UpdateBackgroundBrush();
            }
        }

        public CreditsViewModel Credits
        {
            get
            {
                if (this.Collection == null)
                {
                    this.creditsViewModel.Credits = null;
                }
                else
                {
                    this.creditsViewModel.Credits = this.Collection.Credits;
                }

                return this.creditsViewModel;
            }
        }

        public UpcomingOffersViewModel UpcomingOffers
        {
            get
            {
                this.upcomingOffersViewModel.CollectionID = this.CollectionID;

                return this.upcomingOffersViewModel;
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            FetchCollectionDetails();
        }

        private async void FetchCollectionDetails()
        {
            OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                using (progressService.Show())
                {
                    this.Collection = await connection.GetCollectionDetails(this.CollectionID);
                }
            }
            catch (Exception ex)
            {
                var toast = new ToastPrompt()
                {
                    Title = "Failed to fetch details",
                    Message = ex.Message,
                    TextOrientation = Orientation.Vertical,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.Red),
                };

                toast.Show();
            }
            finally
            {
                OnOperationFinished();
            }
        }

    }
}
