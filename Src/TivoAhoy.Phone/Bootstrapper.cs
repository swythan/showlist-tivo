// From http://www.codeplex.com/Project/Download/FileDownload.aspx?ProjectName=caliburnmicro&DownloadId=140976

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Caliburn.Micro;
using Microsoft.Phone.Controls;
using TivoAhoy.Phone.ViewModels;

namespace TivoAhoy.Phone
{
    public class Bootstrapper : PhoneBootstrapper
    {
        private PhoneContainer container;
        private SterlingService sterlingService;

        protected override void Configure()
        {
            container = new PhoneContainer(RootFrame);

            container.RegisterPhoneServices();
            
            ConventionManager.AddElementConvention<PerformanceProgressBar>(PerformanceProgressBar.IsIndeterminateProperty, "IsIndeterminate", "Loaded");

            sterlingService = new SterlingService();
            container.Instance<ISterlingInstance>(sterlingService);
          
            container.Singleton<SettingsPageViewModel>();
            container.PerRequest<MainPageViewModel>();
            container.PerRequest<ShowDetailsPageViewModel>();

            container.PerRequest<MyShowsViewModel>();
            container.PerRequest<ChannelListViewModel>();
            container.PerRequest<IndividualShowViewModel>();
            container.PerRequest<OfferViewModel>();
            container.PerRequest<ShowContainerViewModel>();
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
            this.sterlingService.Activate();
            base.OnLaunch(sender, e);
        }

        protected override void OnClose(object sender, Microsoft.Phone.Shell.ClosingEventArgs e)
        {
            base.OnClose(sender, e);
            this.sterlingService.Deactivate();
        }

        protected override void OnActivate(object sender, Microsoft.Phone.Shell.ActivatedEventArgs e)
        {
            this.sterlingService.Activate();
            base.OnActivate(sender, e);
        }

        protected override void OnDeactivate(object sender, Microsoft.Phone.Shell.DeactivatedEventArgs e)
        {
            base.OnDeactivate(sender, e);
            this.sterlingService.Deactivate();
        }
    }
}