using System.ComponentModel;
using System.Threading.Tasks;
using Tivo.Connect;

namespace TivoAhoy.Common.Services
{
    public interface ITivoConnectionService : INotifyPropertyChanged
    {
        string ConnectedNetworkName { get; }

        bool SettingsAppearValid { get; }

        bool IsConnected { get; }
        bool IsAwayMode { get; }

        bool IsConnectionEnabled { get; set; }

        Task<TivoConnection> GetConnectionAsync();
    }
}
