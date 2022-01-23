﻿using System;
using System.Text;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using AIT.PE02.Client.Core.Helpers;
using System.IO;
using Newtonsoft.Json;
using System.Collections;

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
        DirectoryInfo[] subdirs;
        FileInfo[] dirfiles;
        Guid guid;

        #region Event Handlers
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                txtUsername.Focus();
                return;
            }
            Connect();
        }
        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }


        #endregion

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

        private void Connect()
        {

            string message = "CONNECT|*|" + txtUsername.Text + "##EOM";
            string response = SendMessageToServer(message);
            if (!string.IsNullOrWhiteSpace(response))
            {
                response = response.Replace("##EOM", "").Trim().ToUpper();
                var parts = response.Split("|*|");
                txbActivePath.Text = parts[5];
                txbGuid.Text = parts[2];
                lstFolders.ItemsSource = (ICollection)JsonConvert.DeserializeObject(parts[3]);
                lstFiles.ItemsSource = (ICollection)JsonConvert.DeserializeObject(parts[4]);
                DoVisuals(true);
            }
            else
            {
                MessageBox.Show("Server unreachable");
                DoVisuals(false);
            }


        }
        private void Disconnect()
        {
            string message = "CLOSE|*|" + txbGuid.Text + "##EOM";
            string response = SendMessageToServer(message);
            if (!string.IsNullOrWhiteSpace(response))
            {
                DoVisuals(false);
            }
            else
            {
                MessageBox.Show("Server unreachable");
                DoVisuals(false);
            }
        }
        public void MKDIR(string mapname)
        {
            string message = $"MKDIR|*|{txbGuid.Text}|*|{mapname}##EOM";
            string response = SendMessageToServer(message);
            if (!string.IsNullOrWhiteSpace(response))
            {
                response = response.Replace("##EOM", "").Trim().ToUpper();
                var parts = response.Split("|*|");

                lstFolders.ItemsSource = (ICollection)JsonConvert.DeserializeObject(parts[1]);

            }
            else
            {
                MessageBox.Show("Server unreachable");
                DoVisuals(false);
            }


        }

        private void DoVisuals(bool isConnected)
        {
            if (isConnected)
            {
                btnDisconnect.Visibility = Visibility.Visible;
                btnConnect.Visibility = Visibility.Hidden;
                grdFTS.Visibility = Visibility.Visible;
                txtIP.IsEnabled = false;
                txtUsername.IsEnabled = false;
            }
            else
            {
                btnDisconnect.Visibility = Visibility.Hidden;
                btnConnect.Visibility = Visibility.Visible;
                txtIP.IsEnabled = true;
                txtIP.Text = null;
                txtUsername.IsEnabled = true;
                grdFTS.Visibility = Visibility.Hidden;
                lstFiles.ItemsSource = null;
                lstFolders.ItemsSource = null;
                txbActivePath = null;
                txbFolderName = null;
                txbFolderParent = null;
                txbFolderpath = null;
                txbFiledate = null;
                txbFilename = null;
                txbFilepath = null;
                txbFilesize = null;
            }
        }

        private void btnAddNewMap_Click(object sender, RoutedEventArgs e)
        {
            MKDIRModal modalWindow = new MKDIRModal();
            modalWindow.ShowDialog();
            if (!modalWindow.IsActive)
            {
                var mapname = MKDIRModal.mapname;
                if (!string.IsNullOrWhiteSpace(mapname))
                {
                    Directory.CreateDirectory(txbActivePath.Text + "\\" + mapname);
                    MKDIR(mapname);
                }
            }
        }
    }
}
