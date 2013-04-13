using System.Net;
namespace TivoAhoy.Phone
{
    public static class ConnectionSettings
    {
        public static string TivoIPAddress
        {
            get { return SettingsStore.GetValueOrDefault(string.Empty, "TivoIPAddress"); }
            set { SettingsStore.AddOrUpdateValue(value, "TivoIPAddress"); }
        }

        public static string MediaAccessKey
        {
            get { return SettingsStore.GetValueOrDefault(string.Empty, "MediaAccessKey"); }
            set { SettingsStore.AddOrUpdateValue(value, "MediaAccessKey"); }
        }

        public static string Username
        {
            get { return SettingsStore.GetValueOrDefault(string.Empty, "Username"); }
            set { SettingsStore.AddOrUpdateValue(value, "Username"); }
        }

        public static string Password
        {
            get { return SettingsStore.GetValueOrDefault(string.Empty, "Password"); }
            set { SettingsStore.AddOrUpdateValue(value, "Password"); }
        }

        public static bool LanSettingsAppearValid(string ipAddress, string mediaAccessKey)
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

            IPAddress parsedIpAddress;
            if (!IPAddress.TryParse(ipAddress, out parsedIpAddress))
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
