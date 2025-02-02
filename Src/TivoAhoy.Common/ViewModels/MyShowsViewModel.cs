﻿//-----------------------------------------------------------------------
// <copyright file="MyShowsViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using Coding4Fun.Toolkit.Controls;
using Microsoft;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Events;
using TivoAhoy.Common.Services;

namespace TivoAhoy.Common.ViewModels
{
    public class MyShowsViewModel : Screen
    {
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;

        private readonly Func<LazyRecordingFolderItemViewModel> showModelFactory;

        private string parentId;
        private IEnumerable<LazyRecordingFolderItemViewModel> myShows;

        public MyShowsViewModel(
            IProgressService progressService,
            ITivoConnectionService connectionService,
            Func<LazyRecordingFolderItemViewModel> showModelFactory)
        {
            this.connectionService = connectionService;
            this.progressService = progressService;

            this.showModelFactory = showModelFactory;

            connectionService.PropertyChanged += OnConnectionServicePropertyChanged;
        }
        
        public MyShowsViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.MyShows = new List<LazyRecordingFolderItemViewModel>
            {
                new LazyRecordingFolderItemViewModel(
                    new IndividualShow()
                    {
                        Title = "The Walking Dead",
                        StartTime = DateTime.Parse("2013/05/01 21:00")                        
                    }),
                new LazyRecordingFolderItemViewModel(
                    new IndividualShow()
                    {
                        Title = "Antiques Roadshow",
                        StartTime = DateTime.Parse("2013/05/02 17:30")                        
                    }),
                new LazyRecordingFolderItemViewModel(
                    new Container()
                    {
                        Title = "64 Zoo Lane",
                        FolderItemCount = 4,
                        FolderType = "series"
                    })
            };
        }

        private void OnConnectionServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
            {
                NotifyOfPropertyChange(() => this.CanRefreshShows);

                if (this.IsActive)
                {
                    this.RefreshShows();
                }
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            NotifyOfPropertyChange(() => this.CanRefreshShows);

            if (this.MyShows == null ||
                !this.MyShows.Any())
            {
                this.RefreshShows();
            }
        }

        public string ParentId
        {
            get { return this.parentId; }
            set
            {
                if (this.parentId == value)
                    return;

                this.parentId = value;

                NotifyOfPropertyChange(() => this.ParentId);
            }
        }

        public IEnumerable<LazyRecordingFolderItemViewModel> MyShows
        {
            get
            {
                return this.myShows;
            }

            private set
            {
                this.myShows = value;
                NotifyOfPropertyChange(() => this.MyShows);
            }
        }

        public bool CanRefreshShows
        {
            get
            {
                return this.connectionService.IsConnected;
            }
        }

        public void RefreshShows()
        {
            if (this.CanRefreshShows)
            {
                FetchShows();
            }
        }

        private async void FetchShows()
        {
            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                using (this.progressService.Show())
                {
                    var ids = await connection.GetRecordingFolderItemIds(this.ParentId);
                    this.MyShows = ids
                        .Select(CreateShowViewModel)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Execute.BeginOnUIThread(() =>
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Failed to fetch My Shows list.",
                        Message = ex.Message,
                        TextOrientation = Orientation.Vertical,
                        TextWrapping = TextWrapping.Wrap,
                        Background = new SolidColorBrush(Colors.Red),
                    };

                    toast.Show();
                });
            }
        }

        private LazyRecordingFolderItemViewModel CreateShowViewModel(long itemId)
        {
            var model = showModelFactory();
            model.Initialise(itemId);

            return model;
        }

        public void ActivateItem(object item)
        {
            //var showContainer = item as ShowContainerViewModel;

            //if (showContainer != null)
            //{
            //    FetchShows(showContainer.Source);
            //}
        }
    }
}
