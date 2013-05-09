using System;
using System.Collections.Generic;

namespace TivoAhoy.Phone
{
    public class AnalyticsService : IAnalyticsService
    {
        private const string EVENT_UnhandledException = "Unhandled Exception";
        private const string EVENT_Connected = "Connected";
        private const string EVENT_PlayRecording = "Play Recording";
        private const string EVENT_DeleteRecording = "Delete Recording";
        private const string EVENT_CancelSingleRecording = "Cancel Single Recording";
        private const string EVENT_ScheduleSingleRecording = "Schedule Single Recording";

        LocalyticsSession session;

        public void OpenSession()
        {
            // Release key
            // this.session = new LocalyticsSession("9abdb358ac0ba50ec8572ed-8c572854-b81f-11e2-0d7e-004a77f8b47f");

            // Debugging key
            this.session = new LocalyticsSession("388a340816ebda0f44d732d-1bf26e3e-b820-11e2-0d7e-004a77f8b47f");

            this.session.open();
            this.session.upload();
        }

        public void CloseSession()
        {
            this.session.close();
        }

        public void TagEvent(string eventName)
        {
            if (this.session == null)
            {
                return;
            }

            this.session.tagEvent(eventName);
        }

        public void TagEvent(string eventName,Dictionary<string, string> attributes)
        {
            if (this.session == null)
            {
                return;
            }

            this.session.tagEvent(eventName, attributes);
        }

        public void AppCrash(Exception exception)
        {
            var attributes = new Dictionary<string, string>()
                {
                    {"exceptionMessage", exception.Message},
                    {"exceptionStack", exception.StackTrace},
                };

            this.TagEvent(EVENT_UnhandledException, attributes);
        }
        
        public void ConnectedAwayMode()
        {
            Connected("away");
        }

        public void ConnectedHomeMode()
        {
            Connected("home");
        }

        private void Connected(string mode)
        {
            var attributes = new Dictionary<string, string>()
                {
                    {"mode", mode},
                };

            this.TagEvent(EVENT_Connected, attributes);
        }


        public void PlayRecording()
        {
            this.TagEvent(EVENT_PlayRecording);
        }

        public void DeleteRecording()
        {
            this.TagEvent(EVENT_DeleteRecording);
        }

        public void CancelSingleRecording()
        {
            this.TagEvent(EVENT_CancelSingleRecording);
        }

        public void ScheduleSingleRecording()
        {
            this.TagEvent(EVENT_ScheduleSingleRecording);
        }
    }
}
