using AIT.PE02.Client.Core.Helpers;
using AIT.PE02.Server.Core.Entities;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

        #region Event Handlers
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Username required");
                txtUsername.Focus();
                return;
            }
            Connect();
        }
        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }
        private void btnAddNewMap_Click(object sender, RoutedEventArgs e)
        {
            MKDIRModal modalWindow = new MKDIRModal();
            modalWindow.Owner = this;
            modalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
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

        private void btnFolderUp_Click(object sender, RoutedEventArgs e)
        {
            CDUP();
        }

        private void lstFolders_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            //Check if double click is on listboxitem, cause event gets triggered with every double click anywhere inside listbox
            while (obj != null && obj != lstFolders)
            {
                if (obj.GetType() == typeof(ListBoxItem))
                {
                    CDDIR();
                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
        }

        private void lstFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstFolders.SelectedItem != null)
            {
                var folderName = lstFolders.SelectedItem.ToString();
                txbFolderName.Text = folderName;
                txbFolderParent.Text = Directory.GetParent(txbActivePath.Text + "\\" + folderName).FullName;
                txbFolderpath.Text = txbActivePath.Text + "\\" + folderName;
            }
            else
            {
                txbFolderName.Text = "";
                txbFolderParent.Text = "";
                txbFolderpath.Text = "";
            }
        }

        private void lstFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstFiles.SelectedItem != null)
            {
                var fileName = lstFiles.SelectedItem.ToString();
                var file = txbActivePath.Text + "\\" + fileName;
                FileInfo fileInfo = new FileInfo(file);
                txbFiledate.Text = fileInfo.CreationTime.ToString();
                txbFilename.Text = fileInfo.Name;
                txbFilepath.Text = fileInfo.FullName;
                txbFilesize.Text = BytesToReadableString(fileInfo.Length);
            }
            else
            {
                txbFiledate.Text = "";
                txbFilename.Text = "";
                txbFilepath.Text = "";
                txbFilesize.Text = "";
            }
        }

        private void btnFileUpload_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files| *.jpg; *.jpeg; *.png|txt files (*.txt)|*.txt";
            if (openFileDialog.ShowDialog() == true)
            {
                FileInfo file = new FileInfo(openFileDialog.FileName);
                FileFTS fileToSend = new FileFTS { Name = file.Name, Fullpath = file.FullName, CreationTime = file.CreationTime, Filesize = file.Length };
                PUT(fileToSend);
            }
        }

        private void lstFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            //Check if double click is on listboxitem, cause event gets triggered with every double click anywhere inside listbox
            while (obj != null && obj != lstFolders)
            {
                if (obj.GetType() == typeof(ListBoxItem))
                {
                    GET();
                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
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
            for (int port = 49000; port <= 49500; port++)
            {
                cmbPorts.Items.Add(port);
            }
            txtIP.Text = "127.0.0.1";
            cmbPorts.SelectedItem = 49200;
            DoVisuals(false);
        }


        private void Connect()
        {

            string message = "CONNECT|*|" + txtUsername.Text + "##EOM";
            string response = SendMessageToServer(message);
            if (!string.IsNullOrWhiteSpace(response))
            {
                if (response.Contains("ERROR"))
                {
                    MessageBox.Show("ERROR");
                }
                else
                {
                    response = response.Replace("##EOM", "").Trim().ToUpper();
                    var parts = response.Split("|*|");
                    txbActivePath.Text = parts[5];
                    txbGuid.Text = parts[2];
                    lstFolders.ItemsSource = (ICollection)JsonConvert.DeserializeObject(parts[3]);
                    lstFiles.ItemsSource = (ICollection)JsonConvert.DeserializeObject(parts[4]);
                    DoVisuals(true);
                }
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
                if (response.Contains("ERROR"))
                {
                    MessageBox.Show("ERROR");
                }
                else
                {
                    response = response.Replace("##EOM", "").Trim().ToUpper();
                    var parts = response.Split("|*|");

                    lstFolders.ItemsSource = (ICollection)JsonConvert.DeserializeObject(parts[1]);
                }
            }
            else
            {
                MessageBox.Show("Server unreachable");
                DoVisuals(false);
            }
        }
        private void CDUP()
        {
            string message = $"CDUP|*|{txbGuid.Text}##EOM";
            string response = SendMessageToServer(message);
            if (!string.IsNullOrWhiteSpace(response))
            {
                if (response.Contains("ERROR"))
                {
                    MessageBox.Show("ERROR");
                }
                else
                {
                    response = response.Replace("##EOM", "").Trim().ToUpper();
                    var parts = response.Split("|*|");

                    txbActivePath.Text = parts[3];
                    lstFolders.ItemsSource = (ICollection)JsonConvert.DeserializeObject(parts[1]);
                    lstFiles.ItemsSource = (ICollection)JsonConvert.DeserializeObject(parts[2]);

                }
            }
        }
        private void CDDIR()
        {
            string message = $"CDDIR|*|{txbGuid.Text}|*|{lstFolders.SelectedItem}##EOM";
            string response = SendMessageToServer(message);
            if (!string.IsNullOrWhiteSpace(response))
            {
                response = response.Replace("##EOM", "").Trim().ToUpper();
                if (response.Contains("ERROR"))
                {
                    MessageBox.Show("ERROR");
                }
                else
                {
                    var parts = response.Split("|*|");

                    txbActivePath.Text = parts[3];
                    lstFolders.ItemsSource = (ICollection)JsonConvert.DeserializeObject(parts[1]);
                    lstFiles.ItemsSource = (ICollection)JsonConvert.DeserializeObject(parts[2]);
                }
            }
        }
        private void GET()
        {
            string message = $"GET|*|{txbGuid.Text}|*|{lstFiles.SelectedItem}##EOM";
            string response = SendMessageToServer(message);
            if (!string.IsNullOrWhiteSpace(response))
            {
                response = response.Replace("##EOM", "").Trim().ToUpper();
                if (response.Contains("ERROR"))
                {
                    MessageBox.Show("ERROR");
                    return;
                }
                else
                {
                    var parts = response.Split("|*|");
                    var serverFile = JsonConvert.DeserializeObject<FileFTS>(parts[1]);

                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    if (saveFileDialog.ShowDialog() == true && saveFileDialog.FileName != serverFile.Fullpath)
                    {
                        File.Copy(serverFile.Fullpath, saveFileDialog.FileName);
                        MessageBox.Show("Download succesful");
                    }
                }
            }
        }
        private void PUT(FileFTS file)
        {
            {
                var jsonFile = JsonConvert.SerializeObject(file);
                string message = $"PUT|*|{txbGuid.Text}|*|{jsonFile}##EOM";
                string response = SendMessageToServer(message);
                if (!string.IsNullOrWhiteSpace(response))
                {
                    response = response.Replace("##EOM", "").Trim().ToUpper();
                    if (response.Contains("ERROR"))
                    {
                        MessageBox.Show("ERROR");
                    }
                    else
                    {
                        var parts = response.Split("|*|");
                        lstFiles.ItemsSource = (ICollection)JsonConvert.DeserializeObject(parts[1]);
                    }
                }
            }
        }

        private void DoVisuals(bool isConnected)
        {
            if (isConnected)
            {
                cmbPorts.IsEnabled = false;
                btnDisconnect.Visibility = Visibility.Visible;
                btnConnect.Visibility = Visibility.Hidden;
                grdFTS.Visibility = Visibility.Visible;
                txtIP.IsEnabled = false;
                txtUsername.IsEnabled = false;
                btnFileUpload.IsEnabled = true;
            }
            else
            {
                cmbPorts.IsEnabled = true;
                btnFileUpload.IsEnabled = false;
                btnDisconnect.Visibility = Visibility.Hidden;
                btnConnect.Visibility = Visibility.Visible;
                txtIP.IsEnabled = true;
                txtUsername.IsEnabled = true;
                grdFTS.Visibility = Visibility.Hidden;
                lstFiles.ItemsSource = null;
                lstFolders.ItemsSource = null;
                txbActivePath.Text = "";
                txbFolderName.Text = "";
                txbFolderParent.Text = "";
                txbFolderpath.Text = "";
                txbFiledate.Text = "";
                txbFilename.Text = "";
                txbFilepath.Text = "";
                txbFilesize.Text = "";
            }
        }
        private string BytesToReadableString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        private void btnOpenNewClientWindow_Click(object sender, RoutedEventArgs e)
        {
            MainWindow clientWindow = new MainWindow();
            clientWindow.Show();
        }
    }
}
