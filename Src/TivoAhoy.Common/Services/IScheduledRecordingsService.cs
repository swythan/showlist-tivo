using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Tivo.Connect.Entities;

namespace TivoAhoy.Common.Services
{
    public interface IScheduledRecordingsService : INotifyPropertyChanged
    {
        bool CanRefreshRecordings { get; }
        Task RefreshRecordings();

        bool IsOfferRecordingScheduled(string offerId);
        Recording GetScheduledRecordingForOffer(string offerId);

        IEnumerable<Recording> ScheduledRecordings { get; }
    }
}
