using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Minecraft_Server_manager_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker BGWorker = new BackgroundWorker();
        private ulong systemRam;
        private Timer timer = new Timer();
        private Timer backupTimer = new Timer();
        private long timerRuntime = 0;
        string timeToDisplay = "";
        int backupTimerInt = 3600;
        Timer TestRamTimer = new Timer();
        double[] ramGraphArray = new double[50];
        MinecraftServer TheServer;
        ObservableCollection<Player> PlayerList { get; set; }
        bool UpdatePlayers = false;



        public MainWindow()
        {
            InitializeComponent();

            #region Initialize 

            BGWorker.WorkerReportsProgress = true;
            BGWorker.WorkerSupportsCancellation = true;
            BGWorker.DoWork += new DoWorkEventHandler(BGWorker_DoWork);
            BGWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGWorker_RunWorkerCompleted);
            BGWorker.ProgressChanged += new ProgressChangedEventHandler(BGWorker_ProgressChanged);
            systemRam = GetTotalMemoryInBytes();
            RamAmountLabel.Content = "MB : " + Properties.Settings.Default.RamAllocation.ToString();
            RamSelectionSlider.SelectionStart = 0;
            RamSelectionSlider.SelectionEnd = (int)((systemRam / 1024) / 1024);
            RamSelectionSlider.Minimum = 0;
            RamSelectionSlider.Maximum = (int)((systemRam / 1024) / 1024);
            RamSelectionSlider.Value = Properties.Settings.Default.RamAllocation;
            ServerLocationTextBox.Text = Properties.Settings.Default.ServerLocation;
            ImportSettingsText();
            timer.Interval = 1;
            timer.Tick += new EventHandler(timerTickUpdate);
            backupTimer.Tick += new EventHandler(backupTimerTickUpdate);
            backupTimer.Interval = 1000;
            LastBackupLabel.Content = Properties.Settings.Default.LastBackup;
            TestRamTimer.Tick += new EventHandler(TestRamTimerTick);
            TestRamTimer.Interval = 500;
            InitalizeRamGraphArray();
            PlayerList = new ObservableCollection<Player>();

            PlayersListView.ItemsSource = PlayerList;

            OnStartupUsers();


            #endregion
        }

        #region Program Startup functions

        private ulong GetTotalMemoryInBytes()
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
        }

        private void InitalizeRamGraphArray()
        {
            for (int i = 0; i < ramGraphArray.Length; i++)
            {
                ramGraphArray[i] = 0;
            }
        }

        #endregion

        #region Dashboard Tab

        private void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!BGWorker.IsBusy && File.Exists(Properties.Settings.Default.ServerLocation))
            {
                timer.Start();
                UptimeStatusLabel.Content = "Server Uptime :";
                StartServerButton.IsEnabled = false;
                StopServerButton.IsEnabled = true;
                CommandTextBox.IsEnabled = true;
                SendCommandButton.IsEnabled = true;
                SelectedPlayerActionButton.IsEnabled = true;
                TestRamTimer.Start();
                BGWorker.RunWorkerAsync();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("There was an error trying to start the server. Please make sure you have a valid server selected.");
            }
        }

        private void StopServerButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            TestRamTimer.Stop();
            TheServer.StopServer();
            UptimeStatusLabel.Content = "Server Offline";
            ServerUptimeLabel.Content = "";
            System.Windows.Forms.MessageBox.Show($"Server has been shutdown after {timeToDisplay}.", "Server shutdown", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            StartServerButton.IsEnabled = true;
            StopServerButton.IsEnabled = false;
            ConsoleLogTextBox.Clear();
            CommandTextBox.IsEnabled = false;
            SendCommandButton.IsEnabled = false;
            SelectedPlayerActionButton.IsEnabled = false;
            CanvasGraph.Children.Clear();
            InitalizeRamGraphArray();
            TotalrRamLabel.Content = "Memoery Usage : ";
        }

        private void StartBackupsButton_Click(object sender, RoutedEventArgs e)
        {
            BackupTimerLabel.Content = "1 : 0 : 0";
            backupTimer.Start();
        }



        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            TabController.SelectedIndex = 3;
        }

        private void TestRamTimerTick(object sender, EventArgs e)
        {
            Process Proc = TheServer.ServerP;
            long memsize = 0; // memsize in Kilabytes
            PerformanceCounter PC = new PerformanceCounter();
            PC.CategoryName = "Process";
            PC.CounterName = "Working Set - Private";
            PC.InstanceName = Proc.ProcessName;
            memsize = (long)PC.NextValue();
            PC.Close();
            PC.Dispose();

            var graphLine = new Polyline
            {
                Stroke = System.Windows.Media.Brushes.Black,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                UseLayoutRounding = true,
                StrokeThickness = 2,
                Fill = System.Windows.Media.Brushes.BlanchedAlmond
            };
            int temp = (int)(memsize / 1024) / 1024;
            for (int i = 2; i < ramGraphArray.Length; i++)
            {
                ramGraphArray[i - 1] = ramGraphArray[i];
            }
            ramGraphArray[ramGraphArray.Length - 2] = temp;

            PointCollection pcollection = new PointCollection();
            for (int i = 0; i < ramGraphArray.Length; i++)
            {
                string s = $"point{i}";
                pcollection.Add(new System.Windows.Point(((CanvasGraph.ActualWidth / ramGraphArray.Length) * i), CanvasGraph.Height - ((ramGraphArray[i] / Properties.Settings.Default.RamAllocation) * CanvasGraph.Height)));
            }

            RamTestLabel.Content = $"Memoery Usage : {temp} MB";
            graphLine.Points = pcollection;

            CanvasGraph.Children.Clear();
            CanvasGraph.Children.Add(graphLine);
        }

        #endregion

        #region Console Do Work

        private void BGWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string serverFile = Properties.Settings.Default.ServerLocation;
            int ramAmount = Properties.Settings.Default.RamAllocation;
            TheServer = new MinecraftServer(serverFile, ramAmount, Properties.Settings.Default.ServerFolder);
            TheServer.StartServer();

            while (TheServer.ServerStatus())
            {
                BGWorker.ReportProgress(0, (string)(TheServer.ServerP.StandardOutput.ReadLine().ToString()));
            }

        }

        private void BGWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string s = (string)e.UserState;
            ConsoleLogTextBox.AppendText(s);
            ConsoleLogTextBox.AppendText(Environment.NewLine);
            ConsoleLogTextBox.ScrollToEnd();
            CheckPlayerConnection(s);
            CheckPlayerDisconnect(s);
            if (UpdatePlayers == true)
            {
                UpdatePlayerList(s);
            }

        }

        private void BGWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        private void SendCommandButton_Click(object sender, RoutedEventArgs e)
        {
            TheServer.SendCommand(CommandTextBox.Text);
            ConsoleLogTextBox.AppendText($"SYSTEM COMMAND SENT : {CommandTextBox.Text}");
            ConsoleLogTextBox.AppendText(Environment.NewLine);
            ConsoleLogTextBox.ScrollToEnd();
            CommandTextBox.Clear();
        }

        private void CheckPlayerConnection(string s)
        {
            Regex rej = new Regex(@"[A-Za-z]+ joined the game", RegexOptions.Compiled);

            if (rej.IsMatch(s))
            {
                MatchCollection matches = rej.Matches(s);
                string[] words = matches[0].ToString().Split(' ');
                int indexcount = 0;
                int index = 0;
                bool playerExists = false;
                foreach (Player p in PlayerList)
                {
                    if (p.Username.ToLower() == words[0].ToLower())
                    {
                        index = indexcount;
                        playerExists = true;
                    }
                    indexcount++;
                }
                if (playerExists)
                {
                    PlayerList[index].SetOnlineStatus(true);
                    RefreshPlayerListView();
                }
                else
                {
                    PlayerList.Add(new Player(words[0], true));
                    RefreshPlayerListView();
                }

            }
        }

        private void CheckPlayerDisconnect(string s)
        {
            Regex rej = new Regex(@"[A-Za-z]+ left the game", RegexOptions.Compiled);

            if (rej.IsMatch(s))
            {
                MatchCollection matches = rej.Matches(s);
                string[] words = matches[0].ToString().Split(' ');
                int indexcount = 0;
                int index = 0;
                bool playerExists = false;
                foreach (Player p in PlayerList)
                {
                    if (p.Username.ToLower() == words[0].ToLower())
                    {
                        index = indexcount;
                        playerExists = true;
                    }
                    indexcount++;
                }
                if (playerExists)
                {
                    PlayerList[index].SetOnlineStatus(false);
                    RefreshPlayerListView();

                }
                else
                {
                    PlayerList.Add(new Player(words[0], false));
                    RefreshPlayerListView();
                }

            }
        }

        private void UpdatePlayerList(string s)
        {
            Regex rej = new Regex(@"There are [0-9]+/[0-9]+ players online", RegexOptions.Compiled);

            if (rej.IsMatch(s))
            {

            }
        }

        #endregion

        #region Users Tab

        private void OnStartupUsers()
        {
            if (Directory.Exists(Properties.Settings.Default.PlayerDataLocation))
                {
                    List<string> tempList = Directory.GetFiles(Properties.Settings.Default.PlayerDataLocation).ToList();
                    foreach (string s in tempList)
                    {
                        PlayerList.Add(new Player(System.IO.Path.GetFileNameWithoutExtension(s)));
                    }
            }
        }

        private void PlayersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Player temp = (Player)PlayersListView.SelectedItem;
            SelectedPlayerLabel.Content = $"Selected Player: {temp.Username}";
        }

        private void RefreshPlayersButton_Click(object sender, RoutedEventArgs e)
        {
            TheServer.SendCommand("list");
            string response = TheServer.GetOutput();
            System.Windows.Forms.MessageBox.Show($"{response}");
        }

        private void SelectedPlayerActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayersListView.SelectedItems.Count == 1)
            {
                DialogResult res = System.Windows.Forms.MessageBox.Show("Are you sure you want to preform this action?", "Action Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == System.Windows.Forms.DialogResult.Yes)
                {

                    Player TP = (Player)PlayersListView.SelectedItem;
                    TheServer.SendCommand($"{PlayerActionsComboBox.Text.ToLower()} {TP.Username}");
                }
            }

        }
        private void RefreshPlayerListView()
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(PlayerList);
            view.Refresh();
        }


        #endregion

        #region Settings Tab

        private void RamSelectionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RamSelectionSlider.Value < 1024)
                RamAmountLabel.Content = $"MB : {RamSelectionSlider.Value}";
            else
                RamAmountLabel.Content = $"GB : {RamSelectionSlider.Value / 1024}";
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(Properties.Settings.Default.ServerLocation))
                {
                    using (StreamWriter SW = new StreamWriter($"{Properties.Settings.Default.ServerFolder}\\server.properties"))
                    {
                        SW.Write(SettingsFileTextBox.Text);
                    }
                }
                if (File.Exists(ServerLocationTextBox.Text))
                {
                    Properties.Settings.Default.ServerLocation = ServerLocationTextBox.Text;

                    try
                    {
                        Properties.Settings.Default.RamAllocation = (int)RamSelectionSlider.Value;
                    }
                    catch (Exception)
                    {
                        System.Windows.Forms.MessageBox.Show("Ram amount was invalid. If the slider shows decimals please make it a whole number.");

                    }
                    Properties.Settings.Default.ServerFolder = System.IO.Path.GetDirectoryName(Properties.Settings.Default.ServerLocation);

                    string pLocation = (string)(System.IO.Path.GetDirectoryName(ServerLocationTextBox.Text) + "\\world\\data\\ftb_lib\\players");

                    if (Directory.Exists(pLocation))
                    {
                        Properties.Settings.Default.PlayerDataLocation = pLocation;
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("player data location was invalid");
                    }


                    Properties.Settings.Default.Save();

                    ImportSettingsText();
                    System.Windows.Forms.MessageBox.Show($"Ram Allocation : {RamSelectionSlider.Value} MB \nServer Location : {ServerLocationTextBox.Text} \nPlayer Data Location : {pLocation}", "Server Settings Updated");
                }
                else
                    System.Windows.Forms.MessageBox.Show($"You did not select a valid server file. Please try again.");
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private void SelectServerLocationButton_Click(object sender, RoutedEventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Java Files (*.jar)|*.jar|All files (*.*)|*.*";
                ofd.RestoreDirectory = true;
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ServerLocationTextBox.Text = ofd.FileName;
                }
            }
        }

        private void ImportSettingsText()
        {
            SettingsFileTextBox.Clear();
            string file = $"{Properties.Settings.Default.ServerFolder}\\server.properties";
            if (File.Exists(file))
            {
                using (StreamReader rd = File.OpenText(file))
                {
                    string s = "";
                    while ((s = rd.ReadLine()) != null)
                    {
                        SettingsFileTextBox.AppendText($"{s}\n");
                    }
                }
            }

        }

        #endregion

        #region Small Client Updates

        private void UpdateTitle(object sender, RoutedEventArgs e)
        {
            TabItem tempTabItem = (TabItem)sender;
            MainWindow1.Title = "MC Server Manager 2 - " + tempTabItem.Header.ToString();

        }

        private void timerTickUpdate(object sender, EventArgs e)
        {
            timerRuntime += 1;

            long runtimeInSeconds = timerRuntime / 100;
            if (runtimeInSeconds < 60)
            {
                timeToDisplay = $"{runtimeInSeconds}";

            }
            else
            {
                long runtimeInMinutes = runtimeInSeconds / 60;
                if (runtimeInMinutes < 1)
                {
                    timeToDisplay = $"{runtimeInSeconds}";
                }
                else
                {
                    int runtimeInHours = (int)(runtimeInMinutes / 60);

                    if (runtimeInHours < 1)
                    {
                        timeToDisplay = $"{runtimeInMinutes}:{runtimeInSeconds % 60}";
                    }
                    else
                    {
                        timeToDisplay = $"{runtimeInHours}:{runtimeInMinutes % 60}:{runtimeInSeconds % 60}";
                    }
                }

            }
            ServerUptimeLabel.Content = timeToDisplay;

        }

        private void backupTimerTickUpdate(object sender, EventArgs e)
        {
            backupTimerInt--;
            int seconds = backupTimerInt % 60;
            int minutes = (backupTimerInt / 60) % 60;
            int hours = ((backupTimerInt / 60) / 60) % 60;
            BackupTimerLabel.Content = $"{hours} : {minutes} : {seconds}";

            if (backupTimerInt == 0)
            {
                TheServer.ServerP.StandardInput.WriteLine("/say Server backup starting server may lag.");
                TheServer.ServerP.StandardInput.WriteLine("/backup start");
                backupTimerInt = 3600;
                DateTime localTime = DateTime.Now;
                LastBackupLabel.Content = localTime;
                Properties.Settings.Default.LastBackup = localTime;
                Properties.Settings.Default.Save();
            }

        }





        #endregion

        private void TestshitButton_Click(object sender, RoutedEventArgs e)
        {
            PlayerList[7].SetOnlineStatus(true);

        }
    }
}
