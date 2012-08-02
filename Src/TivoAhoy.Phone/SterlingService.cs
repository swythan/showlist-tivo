using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Wintellect.Sterling;
using System.ComponentModel;
using System.Diagnostics;
using Tivo.Connect;
using Wintellect.Sterling.IsolatedStorage;

namespace TivoAhoy.Phone
{
    public class SterlingService : ISterlingInstance
    {
        private SterlingEngine _engine;

        private Guid _loggerId = Guid.Empty;

        public ISterlingDatabaseInstance Database { get; private set; }

        private SterlingDefaultLogger _logger;


        public SterlingService()
        {

        }

        public void Activate()
        {
            if (DesignerProperties.IsInDesignTool)
                return;

            if (Debugger.IsAttached)
            {
                _logger = new SterlingDefaultLogger(SterlingLogLevel.Verbose);
            }

            _engine = new SterlingEngine();
            _engine.Activate();

            Database = _engine.SterlingDatabase.RegisterDatabase<TivoInfoCacheDatabase>(new IsolatedStorageDriver());

            Database.Purge();
            //TRIGGERS HERE ETC...
        }

        public void Deactivate()
        {
            if (Debugger.IsAttached && _logger != null)
                _logger.Detach();

            _engine.Dispose();

            Database = null;
            _engine = null;
        }

    }

}
