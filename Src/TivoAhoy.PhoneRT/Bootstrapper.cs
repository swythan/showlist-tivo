// From http://www.codeplex.com/Project/Download/FileDownload.aspx?ProjectName=caliburnmicro&DownloadId=140976

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using Caliburn.Micro;
using Microsoft.Phone.Controls;
using TivoAhoy.Common.Services;
using TivoAhoy.Common.ViewModels;
using TivoAhoy.PhoneRT.Services;

namespace TivoAhoy.PhoneRT
{
    public class Bootstrapper : PhoneBootstrapper
    {
        private PhoneContainer container;

        protected override void Configure()
        {
            container = new PhoneContainer(RootFrame);

            container.RegisterPhoneServices();

            container.Instance<IProgressService>(new ProgressService(RootFrame));

            container.Singleton<IAnalyticsService, AnalyticsService>();
            container.Singleton<ITivoConnectionService, TivoConnectionService>();
            container.Singleton<IScheduledRecordingsService, ScheduledRecordingsService>();
            container.Singleton<ISpeechService, SpeechService>();

            container.PerRequest<SettingsPageViewModel>();
            container.PerRequest<MainPageViewModel>();
            container.PerRequest<ShowContainerShowsPageViewModel>();
            container.PerRequest<CollectionDetailsPageViewModel>();
            container.PerRequest<ShowDetailsPageViewModel>();

            container.PerRequest<MyShowsViewModel>();
            container.PerRequest<ChannelListViewModel>();
            container.PerRequest<ToDoListViewModel>();
            container.PerRequest<SearchViewModel>();
            container.PerRequest<IndividualShowViewModel>();
            container.PerRequest<OfferViewModel>();
            container.PerRequest<RecordingViewModel>();
            container.PerRequest<ShowContainerViewModel>();
            container.PerRequest<LazyRecordingFolderItemViewModel>();
            container.PerRequest<PersonItemViewModel>();
            container.PerRequest<CollectionItemViewModel>();

            AddCustomConventions();
        }

        protected override IEnumerable<Assembly> SelectAssemblies()
        {
            var assemblies =
                new List<Assembly> 
                {
                    typeof(Bootstrapper).Assembly, 
                    typeof(MainPageViewModel).Assembly
                };

            AssemblySource.Instance.AddRange(assemblies);

            return base.SelectAssemblies();
        }

        protected override object GetInstance(Type service, string key)
        {
            return container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }

        protected override void OnLaunch(object sender, Microsoft.Phone.Shell.LaunchingEventArgs e)
        {
            EnableAnalytics(true);          
            EnableConnections(true);

            base.OnLaunch(sender, e);
        }
    
        private void EnableConnections(bool enable)
        {
            var connectionService = (ITivoConnectionService)this.container.GetInstance(typeof(ITivoConnectionService), null);
            connectionService.IsConnectionEnabled = enable;
        }

        private void EnableAnalytics(bool enable)
        {
            var analytics = (IAnalyticsService)this.container.GetInstance(typeof(IAnalyticsService), null);

            if (enable)
            {
                analytics.OpenSession();
            }
            else
            {
                analytics.CloseSession();
            }
        }

        protected override void OnClose(object sender, Microsoft.Phone.Shell.ClosingEventArgs e)
        {
            base.OnClose(sender, e);

            EnableConnections(false);
            EnableAnalytics(false);
        }

        protected override void OnActivate(object sender, Microsoft.Phone.Shell.ActivatedEventArgs e)
        {
            EnableAnalytics(true);
            EnableConnections(true);

            base.OnActivate(sender, e);
        }

        protected override void OnDeactivate(object sender, Microsoft.Phone.Shell.DeactivatedEventArgs e)
        {
            base.OnDeactivate(sender, e);

            EnableConnections(false);
            EnableAnalytics(false);
        }

        protected override void OnUnhandledException(object sender, System.Windows.ApplicationUnhandledExceptionEventArgs e)
        {
            try
            {
                var analytics = (IAnalyticsService)this.container.GetInstance(typeof(IAnalyticsService), null);
                analytics.AppCrash(e.ExceptionObject);
            }
            catch (Exception)
            {
                // Ignore errors when logging
            }

            base.OnUnhandledException(sender, e);
        }

        static void AddCustomConventions()
        {
            ViewModelLocator.AddNamespaceMapping("TivoAhoy.PhoneRT.Views", "TivoAhoy.Common.ViewModels", "View");
            ViewModelLocator.AddNamespaceMapping("TivoAhoy.PhoneRT.Views", "TivoAhoy.Common.ViewModels", "Page");
            ViewLocator.AddNamespaceMapping("TivoAhoy.Common.ViewModels", "TivoAhoy.PhoneRT.Views", "View");
            ViewLocator.AddNamespaceMapping("TivoAhoy.Common.ViewModels", "TivoAhoy.PhoneRT.Views", "Page");

            //ConventionManager.AddElementConvention<PerformanceProgressBar>(PerformanceProgressBar.IsIndeterminateProperty, "IsIndeterminate", "Loaded");

            ConventionManager.AddElementConvention<Pivot>(Pivot.ItemsSourceProperty, "SelectedItem", "SelectionChanged").ApplyBinding =
                (viewModelType, path, property, element, convention) =>
                {
                    if (ConventionManager
                        .GetElementConvention(typeof(ItemsControl))
                        .ApplyBinding(viewModelType, path, property, element, convention))
                    {
                        ConventionManager
                            .ConfigureSelectedItem(element, Pivot.SelectedItemProperty, viewModelType, path);
                        ConventionManager
                            .ApplyHeaderTemplate(element, Pivot.HeaderTemplateProperty, null, viewModelType);
                        return true;
                    }

                    return false;
                };

            ConventionManager.AddElementConvention<Panorama>(Panorama.ItemsSourceProperty, "SelectedItem", "SelectionChanged").ApplyBinding =
                (viewModelType, path, property, element, convention) =>
                {
                    if (ConventionManager
                        .GetElementConvention(typeof(ItemsControl))
                        .ApplyBinding(viewModelType, path, property, element, convention))
                    {
                        ConventionManager
                            .ConfigureSelectedItem(element, Panorama.SelectedItemProperty, viewModelType, path);
                        ConventionManager
                            .ApplyHeaderTemplate(element, Panorama.HeaderTemplateProperty, null, viewModelType);
                        return true;
                    }

                    return false;
                };

            ConventionManager.AddElementConvention<ListPicker>(ListPicker.ItemsSourceProperty, "SelectedItem", "SelectionChanged")
                .ApplyBinding =
                    (viewModelType, path, property, element, convention) =>
                    {
                        if (ConventionManager.GetElementConvention(typeof(ItemsControl))
                            .ApplyBinding(viewModelType, path, property, element, convention))
                        {
                            ConventionManager
                                .ConfigureSelectedItem(element, ListPicker.SelectedItemProperty, viewModelType, path);
                            //ConventionManager
                            //    .ApplyHeaderTemplate(element, ListPicker.HeaderTemplateProperty, null, viewModelType);
                            return true;
                        }

                        return false;
                    };
        }
    }
}