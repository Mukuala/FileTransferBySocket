using AIT.PE02.Server.Core.Entities;
using AIT.PE02.Server.Core.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        List<Client> ClientCollection = new List<Client>();
        List<string> subdirlist = new List<string>();
        List<string> dirfileslist = new List<string>();

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
        private string ProcessClientCall(string instruction)
        {
            string[] parts;
            string returnValue = "";

            instruction = instruction.Replace("##EOM", "").Trim().ToUpper();

            //Gets called when you connect to server
            if (instruction.Length > 7 && instruction.Substring(0, 7) == "CONNECT")
            {
                #region Connect

                parts = instruction.Split("|*|");
                if (parts.Length != 2)
                    return "ERROR##EOM";

                else
                {
                    var client = new Client { Id = Guid.NewGuid(), CurrentMap = txtBasepath.Text, Name = parts[1] };
                    ClientCollection.Add(client);
                    GetAllSubdirsAndFiles(client.CurrentMap, out subdirlist, out dirfileslist);
                    var stringSubdirs = JsonConvert.SerializeObject(subdirlist);
                    var stringDirfiles = JsonConvert.SerializeObject(dirfileslist);
                    txbCommunications.Text = $"{parts[1]} (id={client.Id}) CONNECTED, AP = {client.CurrentMap}\n" + txbCommunications.Text;
                    returnValue = $"CONNECT|*|{parts[1]}|*|{client.Id}|*|{stringSubdirs}|*|{stringDirfiles}|*|{client.CurrentMap}##EOM";
                    return returnValue;
                }
                #endregion
            }

            //Gets called when you close or disconnect to server
            else if (instruction.Length > 5 && instruction.Substring(0, 5) == "CLOSE")
            {
                #region Close
                parts = instruction.Split("|*|");
                if (parts.Length != 2)
                    return "ERROR##EOM";

                var client = GetCurrentClient(parts[1]);
                txbCommunications.Text = $"{client.Name} (id={client.Id}) DISCONNECTED, AP = {client.CurrentMap}\n" + txbCommunications.Text;
                ClientCollection.Remove(client);
                return "CLOSE##EOM";
                #endregion
            }

            //Gets called when u make a new directory
            else if (instruction.Length > 5 && instruction.Substring(0, 5) == "MKDIR")
            {
                #region MKDIR
                parts = instruction.Split("|*|");
                if (parts.Length != 3)
                    return "ERROR##EOM";

                var client = GetCurrentClient(parts[1]);
                GetAllSubdirsAndFiles(client.CurrentMap, out subdirlist, out dirfileslist);
                var stringSubdirs = JsonConvert.SerializeObject(subdirlist);
                txbCommunications.Text = $"{client.Name} (id={client.Id}) requested MKDIR {parts[2]}, AP = {client.CurrentMap}\n" + txbCommunications.Text;
                return $"MKDIR|*|{stringSubdirs}|*|{client.CurrentMap}##EOM";
                #endregion
            }

            //Gets called when you move up in directory
            else if (instruction.Length > 4 && instruction.Substring(0, 4) == "CDUP")
            {
                #region CDUP
                string newAP = "";
                parts = instruction.Split("|*|");
                if (parts.Length != 2)
                    return "ERROR##EOM";

                var client = GetCurrentClient(parts[1]);
                OneFolderUp(client.CurrentMap, out subdirlist, out dirfileslist, out newAP);
                var stringSubdirs = JsonConvert.SerializeObject(subdirlist);
                var stringDirfiles = JsonConvert.SerializeObject(dirfileslist);
                client.CurrentMap = newAP;
                txbCommunications.Text = $"{client.Name} (id={client.Id}) requested CDUP, AP = {client.CurrentMap}\n" + txbCommunications.Text;
                returnValue = $"CDUP|*|{stringSubdirs}|*|{stringDirfiles}|*|{client.CurrentMap}##EOM";
                return returnValue;
                #endregion
            }

            //Gets called when you move down in directory (Enter a child directory)
            else if (instruction.Length > 5 && instruction.Substring(0, 5) == "CDDIR")
            {
                #region CDDIR
                string newAP = "";
                parts = instruction.Split("|*|");
                if (parts.Length != 3)
                    return "ERROR##EOM";

                var client = GetCurrentClient(parts[1]);
                newAP = client.CurrentMap + "\\" + parts[2];
                GetAllSubdirsAndFiles(newAP, out subdirlist, out dirfileslist);
                var stringSubdirs = JsonConvert.SerializeObject(subdirlist);
                var stringDirfiles = JsonConvert.SerializeObject(dirfileslist);
                client.CurrentMap = newAP;
                txbCommunications.Text = $"{client.Name} (id={client.Id}) requested CDDIR {parts[2]}, AP = {client.CurrentMap}\n" + txbCommunications.Text;
                return $"CDDIR|*|{stringSubdirs}|*|{stringDirfiles}|*|{client.CurrentMap}##EOM";
                #endregion
            }

            //Gets called when "downloading" file from server
            else if (instruction.Length > 3 && instruction.Substring(0, 3) == "GET")
            {
                #region GET
                parts = instruction.Split("|*|");
                if (parts.Length != 3)
                    return "ERROR##EOM";

                var client = GetCurrentClient(parts[1]);
                FileInfo file = new FileInfo(client.CurrentMap + "\\" + parts[2]);
                FileFTS fileFTS = new FileFTS { Name = file.Name, Fullpath = file.FullName, CreationTime = file.CreationTime, Filesize = file.Length };
                var fileToSend = JsonConvert.SerializeObject(fileFTS);
                txbCommunications.Text = $"{client.Name} (id={client.Id}) requested GET {parts[2]}, AP = {client.CurrentMap}\n" + txbCommunications.Text;
                return $"GET|*|{fileToSend}|*|{client.CurrentMap}##EOM";
                #endregion
            }
            //Gets called when "uploading" file to server
            else if (instruction.Length > 3 && instruction.Substring(0, 3) == "PUT")
            {
                #region PUT
                parts = instruction.Split("|*|");
                if (parts.Length != 3)
                    return "ERROR##EOM";

                var client = GetCurrentClient(parts[1]);
                var fileFTS = JsonConvert.DeserializeObject<FileFTS>(parts[2]);
                File.Copy(fileFTS.Fullpath, client.CurrentMap + "/" + fileFTS.Name);
                GetAllSubdirsAndFiles(client.CurrentMap, out subdirlist, out dirfileslist);
                var stringDirfiles = JsonConvert.SerializeObject(dirfileslist);
                txbCommunications.Text = $"{client.Name} (id={client.Id}) requested PUT {fileFTS.Name}, AP = {client.CurrentMap}\n" + txbCommunications.Text;
                return $"PUT|*|{stringDirfiles}|*|{client.CurrentMap}##EOM";


                #endregion
            }
            return "ERROR##EOM";
        }


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
            CreateBasePathIfNotExist();

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
        private void GetAllSubdirsAndFiles(string path, out List<string> subdirs, out List<string> dirfiles)
        {
            subdirlist.Clear();
            dirfileslist.Clear();

            var mainDirectory = new DirectoryInfo(path);
            var subDirectories = mainDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly);
            var maindirFiles = mainDirectory.GetFiles();
            foreach (var item in subDirectories)
            {
                subdirlist.Add(item.Name);
            }
            foreach (var item in maindirFiles)
            {
                dirfileslist.Add(item.Name);
            }
            subdirs = subdirlist;
            dirfiles = dirfileslist;
        }
        private void OneFolderUp(string AP, out List<string> subdirs, out List<string> dirfiles, out string newAP)
        {
            var folderUp = Directory.GetParent(AP).FullName;
            if (AP.ToUpper() == txtBasepath.Text.ToUpper())
            {
                GetAllSubdirsAndFiles(AP, out subdirlist, out dirfileslist);
                newAP = AP;
                subdirs = subdirlist;
                dirfiles = dirfileslist;
            }
            else
            {
                GetAllSubdirsAndFiles(folderUp, out subdirlist, out dirfileslist);
                newAP = folderUp;
                subdirs = subdirlist;
                dirfiles = dirfileslist;
            }
        }
        private Client GetCurrentClient(string id)
        {
            var parseGuid = Guid.Parse(id);
            var client = ClientCollection.FirstOrDefault(c => c.Id.Equals(parseGuid));
            return client;
        }
        private void CreateBasePathIfNotExist()
        {
            if (!Directory.Exists(txtBasepath.Text))
            {
                Directory.CreateDirectory(txtBasepath.Text);
            }
        }
    }
}
