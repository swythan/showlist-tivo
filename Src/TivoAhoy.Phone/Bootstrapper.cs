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
        PhoneContainer container;

        protected override void Configure()
        {
            container = new PhoneContainer(RootFrame);

            container.RegisterPhoneServices();
            container.Singleton<SettingsPageViewModel>();
            container.PerRequest<MainPageViewModel>();
            container.PerRequest<IndividualShowViewModel>();
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
    }
}