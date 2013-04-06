using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TivoAhoy.Phone
{
    public interface IScheduledRecordingsService : INotifyPropertyChanged
    {
        bool IsOfferRecordingScheduled(string offerId);
    }
}
