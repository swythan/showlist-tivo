//-----------------------------------------------------------------------
// <copyright file="AppBootstrapper.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace TivoProxy
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using Caliburn.Micro;
    using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
    using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;

    public class AppBootstrapper : BootstrapperBase
    {
        SimpleContainer container;

        private ObservableEventListener logListener;

        public AppBootstrapper()
        {
            Start();
        }

        protected override void Configure()
        {
            container = new SimpleContainer();

            container.Singleton<IWindowManager, WindowManager>();
            container.Singleton<IEventAggregator, EventAggregator>();
            container.PerRequest<IShell, ShellViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            var instance = container.GetInstance(service, key);
            if (instance != null)
                return instance;

            throw new InvalidOperationException("Could not locate any instances.");
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
        {
            logListener = new ObservableEventListener();
            logListener.EnableEvents(TivoProxyEventSource.Log, EventLevel.LogAlways, Keywords.All);

            logListener.LogToConsole(new SimpleEventTextFormatter());
            logListener.LogToFlatFile("ProxyLog.txt", new SimpleEventTextFormatter());

            DisplayRootViewFor<IShell>();
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            logListener.DisableEvents(TivoProxyEventSource.Log);
            logListener.Dispose();

            base.OnExit(sender, e);
        }
    }
}
