using System.ComponentModel;
using System.Threading.Tasks;
using Tivo.Connect;

namespace TivoAhoy.Phone
{
    public interface ITivoConnectionService : INotifyPropertyChanged
    {
        bool SettingsAppearValid { get; }

        bool IsConnected { get; }
        
        bool IsConnectionEnabled { get; set; }
        bool IsAwayModeEnabled { get; set; }

        Task<TivoConnection> GetConnectionAsync();
    }
}
