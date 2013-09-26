//-----------------------------------------------------------------------
// <copyright file="IAnalyticsService.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace TivoAhoy.Common.Services
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
