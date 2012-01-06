using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Caliburn.Micro;
using System.Net.Sockets;
using Tivo.Connect;

namespace TivoAhoy.Phone
{
    public class SettingsPageViewModel : PropertyChangedBase
    {
        public SettingsPageViewModel()
        {
            this.TivoIPAddress = "192.168.0.18";
            this.MediaAccessKey = "9837127953";
        }
        
        public string TivoIPAddress { get; set; }
        
        public string MediaAccessKey { get; set; }

        public void TestConnection()
        {
            using (var connection = new TivoConnection())
            {
                try
                {
                    connection.Connect(this.TivoIPAddress, this.MediaAccessKey);

                    MessageBox.Show("Connection Succeeded!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Connection Failed! :-(\n{0}", ex));
                }
            }
        }
    }
}
