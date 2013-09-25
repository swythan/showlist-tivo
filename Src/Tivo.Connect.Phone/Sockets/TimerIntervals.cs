//-----------------------------------------------------------------------
// <copyright file="TimerIntervals.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Threading;

namespace System.Net.Sockets
{
    internal static class TimerIntervals
    {
        public static TimeSpan Never = TimeSpan.FromMilliseconds(Timeout.Infinite);
    }
}
