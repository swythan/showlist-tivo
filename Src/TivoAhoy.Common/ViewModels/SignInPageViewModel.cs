//-----------------------------------------------------------------------
// <copyright file="SettingsPageViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using Coding4Fun.Toolkit.Controls;
using Tivo.Connect;
using TivoAhoy.Common.Events;
using TivoAhoy.Common.Services;
using TivoAhoy.Common.Settings;

namespace TivoAhoy.Common.ViewModels
{
    public class SignInPageViewModel : Screen
    {
        private readonly IEventAggregator eventAggregator;
        private readonly INavigationService navigationService;
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;

        private string username;
        private string password;

        private bool isTestInProgress;

        public SignInPageViewModel(
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            IProgressService progressService,
            ITivoConnectionService connectionService)
        {
            this.eventAggregator = eventAggregator;
            this.connectionService = connectionService;
            this.navigationService = navigationService;
            this.progressService = progressService;
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            this.Username = ConnectionSettings.AwayModeUsername;
            this.Password = ConnectionSettings.AwayModePassword;
        }

        public string Username
        {
            get { return this.username; }
            set
            {
                if (this.username == value)
                    return;

                this.username = value;
                NotifyOfPropertyChange(() => this.Username);
                NotifyOfPropertyChange(() => this.AwaySettingsAppearValid);
                NotifyOfPropertyChange(() => this.CanConnect);
            }
        }

        public string Password
        {
            get { return this.password; }
            set
            {
                if (this.password == value)
                    return;

                this.password = value;
                NotifyOfPropertyChange(() => this.Password);
                NotifyOfPropertyChange(() => this.AwaySettingsAppearValid);
                NotifyOfPropertyChange(() => this.CanConnect);
            }
        }

        public bool IsTestInProgress
        {
            get { return this.isTestInProgress; }
        }

        private void SetIsTestInProgress(bool value)
        {
            if (this.isTestInProgress == value)
            {
                return;
            }

            this.isTestInProgress = value;

            NotifyOfPropertyChange(() => this.IsTestInProgress);
            NotifyOfPropertyChange(() => this.CanConnect);
        }

        private IDisposable ShowProgress()
        {
            this.SetIsTestInProgress(true);

            return new CompositeDisposable(
                this.progressService.Show(),
                Disposable.Create(() => this.SetIsTestInProgress(false)));
        }

        public bool CanConnect
        {
            get
            {
                return this.AwaySettingsAppearValid && !this.IsTestInProgress;
            }
        }

        public bool AwaySettingsAppearValid
        {
            get
            {
                return ConnectionSettings.AwaySettingsAppearValid(this.Username, this.Password);
            }
        }

        public async void Connect()
        {
            ConnectionSettings.AwayModeUsername = this.Username;
            ConnectionSettings.AwayModePassword = this.Password;
            this.eventAggregator.Publish(new ConnectionSettingsChanged());

            this.connectionService.IsConnectionEnabled = true;
            try
            {
                if (await this.connectionService.EnsureConnectedAsync())
                {
                    if (this.navigationService.CanGoBack)
                    {
                        this.navigationService.GoBack();
                    }
                    else
                    {
                        var settingsUri = this.navigationService.UriFor<MainPageViewModel>().BuildUri();

                        this.navigationService.Navigate(settingsUri);
                    }
                }
                else
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Connection Failed",
                        Message = this.connectionService.Error,
                        TextOrientation = Orientation.Vertical,
                        TextWrapping = TextWrapping.Wrap,
                        Background = new SolidColorBrush(Colors.Red),
                    };

                    toast.Show();
                }
            }
            catch (UnauthorizedAccessException)
            {
                var toast = new ToastPrompt()
                {
                    Title = "Connection Failed",
                    Message = "Invalid username or password",
                    TextOrientation = Orientation.Vertical,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.Red),
                };

                toast.Show();
            }
            catch (TivoException ex)
            {
                string message = ex.Message;

                if (ex.Code == "authenticationFailed")
                {
                    message = "Invalid username or password";
                }

                var toast = new ToastPrompt()
                {
                    Title = "Connection Failed",
                    Message = message,
                    TextOrientation = Orientation.Vertical,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.Red),
                };

                toast.Show();
            }
            catch (Exception ex)
            {
                var toast = new ToastPrompt()
                {
                    Title = "Connection Failed",
                    Message = ex.Message,
                    TextOrientation = Orientation.Vertical,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.Red),
                };

                toast.Show();
            }
        }

    }
}
