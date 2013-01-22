using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tivo.Connect;

namespace TivoTest
{
    public interface ITivoConnectionService : INotifyPropertyChanged
    {
        bool IsConnected { get; }
        bool IsAwayModeEnabled { get; set; }

        Task<TivoConnection> GetConnectionAsync();
    }
}
