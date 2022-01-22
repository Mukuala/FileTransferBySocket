using System;
using System.Text;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using AIT.PE02.Client.Core.Helpers;

namespace AIT.PE02.Client.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            btnDisconnect.Visibility = Visibility.Hidden;
            StartupConfig();
        }
        Socket serverSocket;
        IPEndPoint serverEndpoint;

        private string SendMessageToServer(string message)
        {
            if (serverSocket == null)
            {
                IPAddress serverIP = IPAddress.Parse(txtIP.Text);
                int serverPort = int.Parse(cmbPorts.SelectedItem.ToString());
                serverEndpoint = new IPEndPoint(serverIP, serverPort);
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            try
            {
                serverSocket.Connect(serverEndpoint);
                byte[] outMessage = Encoding.ASCII.GetBytes(message);
                byte[] inMessage = new byte[1024];

                serverSocket.Send(outMessage);
                string response = "";
                while (true)
                {
                    int responseLength = serverSocket.Receive(inMessage);
                    response += Encoding.ASCII.GetString(inMessage, 0, responseLength).ToUpper();
                    if (response.IndexOf("##EOM") > -1)
                        break;
                }
                serverSocket.Shutdown(SocketShutdown.Both);
                serverSocket.Close();
                serverSocket = null;
                return response;
            }
            catch (Exception fout)
            {
                serverSocket = null;
                return "";
            }
        }
        private void StartupConfig()
        {
            for (int port = 49200; port <= 49500; port++)
            {
                cmbPorts.Items.Add(port);
            }
            AppConfig.GetConfig(out string serverIP, out int communicationPort);
            txtIP.Text = serverIP;
            try
            {
                cmbPorts.SelectedItem = communicationPort;
            }
            catch
            {
                cmbPorts.SelectedItem = 49200;
            }

        }
        private void SaveConfig()
        {
            AppConfig.WriteConfig(txtIP.Text, int.Parse(cmbPorts.SelectedItem.ToString()));

        }

    }
}
