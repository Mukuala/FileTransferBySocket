using AIT.PE02.Server.Core.Helpers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AIT.PE02.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StartupConfig();

        }
        Socket serverSocket;
        IPEndPoint serverEndpoint;
        bool serverOnline = false;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            BtnStopServer_Click(null, null);
        }
        private void CmbIPs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveConfig();
        }
        private void CmbPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveConfig();
        }

        private void BtnStartServer_Click(object sender, RoutedEventArgs e)
        {
            btnStartServer.Visibility = Visibility.Hidden;
            btnStopServer.Visibility = Visibility.Visible;
            cmbIPs.IsEnabled = false;
            cmbPorts.IsEnabled = false;
            grdCommunications.ItemsSource = null;

            StartTheServer();
            StartListening();
        }
        private void BtnStopServer_Click(object sender, RoutedEventArgs e)
        {
            btnStartServer.Visibility = Visibility.Visible;
            btnStopServer.Visibility = Visibility.Hidden;
            cmbIPs.IsEnabled = true;
            cmbPorts.IsEnabled = true;

            CloseTheServer();
        }


        private void StartTheServer()
        {
            serverOnline = true;
        }
        private void CloseTheServer()
        {
            serverOnline = false;
            try
            {
                if (serverSocket != null)
                    serverSocket.Close();
            }
            catch
            { }
            serverSocket = null;
            serverEndpoint = null;
        }
        private void StartListening()
        {
            IPAddress ip = IPAddress.Parse(cmbIPs.SelectedItem.ToString());
            int port = int.Parse(cmbPorts.SelectedItem.ToString());
            serverEndpoint = new IPEndPoint(ip, port);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                serverSocket.Bind(serverEndpoint);
                serverSocket.Listen(int.MaxValue);
                while (serverOnline)
                {
                    DoEvents();
                    if (serverSocket != null)
                    {
                        if (serverSocket.Poll(200000, SelectMode.SelectRead))
                        {
                            HandleClientCall(serverSocket.Accept());
                        }
                    }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleClientCall(Socket clientCall)
        {
            byte[] clientRequest = new Byte[1024];
            string instruction = null;

            while (true)
            {
                int numByte = clientCall.Receive(clientRequest);
                instruction += Encoding.ASCII.GetString(clientRequest, 0, numByte);
                if (instruction.IndexOf("##EOM") > -1)
                    break;
            }
            string serverResponseInText = ProcessClientCall(instruction);
            if (serverResponseInText != "")
            {
                byte[] serverResponse = Encoding.ASCII.GetBytes(serverResponseInText);
                clientCall.Send(serverResponse);
            }
            clientCall.Shutdown(SocketShutdown.Both);
            clientCall.Close();
        }
        //private string ProcessClientCall(string instruction)
        //{
        //    string[] parts;
        //    string returnValue = "";

        //    instruction = instruction.Replace("##EOM", "").Trim().ToUpper();

        //    if (instruction.Length > 7 && instruction.Substring(0, 7) == "CONNECT")
        //    {
        //        returnValue = SerializeList();
        //        return returnValue + "##EOM";
        //    }
        //    else if (instruction.Length > 3 && instruction.Substring(0, 3) == "GET")
        //    {
        //        parts = instruction.Split('|');
        //        if (parts.Length != 2)
        //            return "Sorry ... I don't understand you ...##EOM";
        //        foreach (Location location in locationService.Locations)
        //        {
        //            if (location.LocationName == parts[1])
        //            {
        //                returnValue = SerializeObject(location);
        //                break;
        //            }
        //        }
        //        if (returnValue == "")
        //        {
        //            return "Sorry ... location unknown ...##EOM";
        //        }
        //        else
        //        {
        //            return returnValue + "##EOM";
        //        }

        //    }
        //    else if (instruction.Length > 3 && instruction.Substring(0, 3) == "PUT")
        //    {
        //        parts = instruction.Split('|');
        //        if (parts.Length != 2)
        //            return "Sorry ... I don't understand you ...##EOM";

        //        Location location = JsonConvert.DeserializeObject<Location>(parts[1]);
        //        locationService.AddObservation(location);
        //        returnValue = SerializeList();
        //        DisplayData();
        //        return returnValue + "##EOM";
        //    }
        //    return "Sorry ... I don't understand you ...##EOM";

        //}


        private void StartupConfig()
        {
            cmbIPs.ItemsSource = IPv4Helper.GetActiveIP4s();
            for (int port = 49200; port <= 49500; port++)
            {
                cmbPorts.Items.Add(port);
            }
            AppConfig.GetConfig(out string savedIP, out int savedPort);
            try
            {
                cmbIPs.SelectedItem = savedIP;
            }
            catch
            {
                cmbIPs.SelectedItem = "127.0.0.1";
            }
            try
            {
                cmbPorts.SelectedItem = savedPort;
            }
            catch
            {
                cmbPorts.SelectedItem = 49200;
            }
            btnStartServer.Visibility = Visibility.Visible;
            btnStopServer.Visibility = Visibility.Hidden;
        }
        private void SaveConfig()
        {
            if (cmbIPs.SelectedItem == null) return;
            if (cmbPorts.SelectedItem == null) return;

            string ip = cmbIPs.SelectedItem.ToString();
            int port = int.Parse(cmbPorts.SelectedItem.ToString());
            AppConfig.WriteConfig(ip, port);
        }
        private static void DoEvents()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            }
            catch (Exception fout)
            {
                Application.Current.Dispatcher.DisableProcessing();
            }
        }


    }
}
