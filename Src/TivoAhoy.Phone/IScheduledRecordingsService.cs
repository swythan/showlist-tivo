using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Tivo.Connect.Entities;

namespace TivoAhoy.Phone
{
    public interface IScheduledRecordingsService : INotifyPropertyChanged
    {
        bool CanRefreshRecordings { get; }
        Task RefreshRecordings();
        
        bool IsOfferRecordingScheduled(string offerId);

        IEnumerable<Recording> ScheduledRecordings { get; }
    }
}
