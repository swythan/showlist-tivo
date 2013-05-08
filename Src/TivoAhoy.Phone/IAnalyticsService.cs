using System;

namespace TivoAhoy.Phone
{
    public interface IAnalyticsService
    {
        void OpenSession();
        void CloseSession();

        void TagEvent(string eventName);

        void AppCrash(Exception exception);

        void ConnectedAwayMode();
        void ConnectedHomeMode();

        void PlayRecording();
        void DeleteRecording();
        void CancelSingleRecording();
        void ScheduleSingleRecording();
    }
}