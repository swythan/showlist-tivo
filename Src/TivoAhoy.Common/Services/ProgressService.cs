namespace TivoAhoy.Common.Services
{
    using System;
    using System.Reactive.Disposables;
    using System.Threading;
    using System.Windows.Navigation;
    using Caliburn.Micro;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Shell;

    public class ProgressService : IProgressService
    {
        private readonly ProgressIndicator progressIndicator;

        private int operationsInProgress = 0;

        public ProgressService(PhoneApplicationFrame rootFrame)
        {
            progressIndicator = new ProgressIndicator();

            rootFrame.Navigated += RootFrameOnNavigated;
        }

        private void RootFrameOnNavigated(object sender, NavigationEventArgs args)
        {
            var content = args.Content;
            var page = content as PhoneApplicationPage;
            if (page == null)
                return;

            page.SetValue(SystemTray.ProgressIndicatorProperty, progressIndicator);
        }

        public IDisposable Show()
        {
            Interlocked.Increment(ref this.operationsInProgress);

            Execute.OnUIThread(UpdateProgressUI);

            return Disposable.Create(() => Hide());
        }

        private void Hide()
        {
            Interlocked.Decrement(ref this.operationsInProgress);

            Execute.OnUIThread(UpdateProgressUI);
        }

        private void UpdateProgressUI()
        {
            if (this.operationsInProgress > 0)
            {
                progressIndicator.IsIndeterminate = true;
                progressIndicator.IsVisible = true;
            }
            else
            {
                progressIndicator.IsIndeterminate = false;
                progressIndicator.IsVisible = false;
            }
        }
    }
}
