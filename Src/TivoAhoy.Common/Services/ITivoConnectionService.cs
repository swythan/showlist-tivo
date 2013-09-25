//-----------------------------------------------------------------------
// <copyright file="ITivoConnectionService.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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
