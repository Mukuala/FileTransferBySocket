﻿using AIT.PE02.Server.Core.Entities;
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

            if (instruction.Length > 7 && instruction.Substring(0, 7) == "CONNECT")
            {
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
                    txbCommunications.Text = $"{parts[1]} ({client.Id}) CONNECTED, AP = {client.CurrentMap}\n" + txbCommunications.Text;
                    returnValue = $"CONNECT|*|{parts[1]}|*|{client.Id}|*|{stringSubdirs}|*|{stringDirfiles}|*|{client.CurrentMap}##EOM";
                    return returnValue;
                }
            }
            else if (instruction.Length > 5 && instruction.Substring(0, 5) == "CLOSE")
            {
                parts = instruction.Split("|*|");
                if (parts.Length != 2)
                    return "ERROR##EOM";

                var client = GetCurrentClient(parts[1]);
                txbCommunications.Text = $"{client.Name} {client.Id} DISCONNECTED, AP = {client.CurrentMap}\n" + txbCommunications.Text;
                ClientCollection.Remove(client);
                return "CLOSE##EOM";
            }
            else if (instruction.Length > 5 && instruction.Substring(0, 5) == "MKDIR")
            {
                parts = instruction.Split("|*|");
                if (parts.Length != 3)
                    return "ERROR##EOM";

                var client = GetCurrentClient(parts[1]);
                GetAllSubdirsAndFiles(client.CurrentMap, out subdirlist, out dirfileslist);
                var stringSubdirs = JsonConvert.SerializeObject(subdirlist);
                txbCommunications.Text = $"{client.Name} ({client.Id}) requested MKDIR {parts[2]}, AP = {client.CurrentMap}\n" + txbCommunications.Text;
                return $"MKDIR|*|{stringSubdirs}|*|{client.CurrentMap}##EOM";

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
            var subDirectories = mainDirectory.GetDirectories("*", SearchOption.AllDirectories);
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
        private Client GetCurrentClient(string id)
        {
            var parseGuid = Guid.Parse(id);
            var client = ClientCollection.FirstOrDefault(c => c.Id.Equals(parseGuid));
            return client;
        }
    }
}
