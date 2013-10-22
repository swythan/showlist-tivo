//-----------------------------------------------------------------------
// <copyright file="ConnectionSettings.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Tivo.Connect;

namespace TivoAhoy.Common.Settings
{
    public static class ConnectionSettings
    {
        public static KnownTivoConnection[] KnownTivos
        {
            get { return SettingsStore.GetValueOrDefault(new KnownTivoConnection[0], "KnownTivos"); }
            set { SettingsStore.AddOrUpdateValue(value, "KnownTivos"); }
        }

        public static string SelectedTivoTsn
        {
            get { return SettingsStore.GetValueOrDefault(string.Empty, "SelectedTivoTsn"); }
            set { SettingsStore.AddOrUpdateValue(value, "SelectedTivoTsn"); }
        }

        public static string AwayModeUsername
        {
            get { return SettingsStore.GetValueOrDefault(string.Empty, "Username"); }
            set { SettingsStore.AddOrUpdateValue(value, "Username"); }
        }

        public static string AwayModePassword
        {
            get { return SettingsStore.GetValueOrDefault(string.Empty, "Password"); }
            set { SettingsStore.AddOrUpdateValue(value, "Password"); }
        }

        public static TivoServiceProvider AwayModeServiceProvider
        {
            get { return SettingsStore.GetValueOrDefault(GetDefaultServiceProviderByLanguage(), "AwayModeServiceProvider"); }
            set { SettingsStore.AddOrUpdateValue(value, "AwayModeServiceProvider"); }
        }

        private static TivoServiceProvider GetDefaultServiceProviderByLanguage()
        {
            // default awayServiceProvider to defaults based on language
            var culture = CultureInfo.CurrentUICulture;
            if(culture.Name.StartsWith("en-US"))
                return TivoServiceProvider.TivoUSA;
            
            return TivoServiceProvider.VirginMediaUK; 
        }

        public static bool LanSettingsAppearValid(IPAddress ipAddress, string mediaAccessKey)
        {
            if (mediaAccessKey == null ||
                mediaAccessKey.Length != 10)
            {
                return false;
            }

            long makAsLong;
            if (!long.TryParse(mediaAccessKey, out makAsLong))
            {
                return false;
            }

            if (ipAddress == null ||
                ipAddress == IPAddress.None)
            {
                return false;
            }

            return true;
        }

        public static bool AwaySettingsAppearValid(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            return true;
        }
    }
}
