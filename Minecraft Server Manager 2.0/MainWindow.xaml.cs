using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Collections;
using Renci.SshNet;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Charts;



namespace Minecraft_Server_Manager_2._0
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
        Process p = new Process();
        int backupTimerInt = 3600;
        Timer TestRamTimer = new Timer();
        Random rand = new Random();
        bool ramTimerBool = false;
        double[] ramGraphArray = new double[20] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        bool ServerRunning = false;


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
            TestRamTimer.Interval = 1000;

            #endregion
        }

        

        #region Program Startup functions

        private ulong GetTotalMemoryInBytes()
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
        }

        #endregion

        #region Dashboard

        private void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!BGWorker.IsBusy && File.Exists(Properties.Settings.Default.ServerLocation))
            {
                timer.Start();
                UptimeStatusLabel.Content = "Server Uptime :";
                StartServerButton.IsEnabled = false;
                StopServerButton.IsEnabled = true;
                BGWorker.RunWorkerAsync();
                CommandTextBox.IsEnabled = true;
                SendCommandButton.IsEnabled = true;
                ServerRunning = true;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("There was an error trying to start the server. Please make sure you have a valid server selected.");
            }
        }

        private void StopServerButton_Click(object sender, RoutedEventArgs e)
        {
            p.StandardInput.WriteLine("stop");
            p.CloseMainWindow();
            timer.Stop();
            UptimeStatusLabel.Content = "Server Offline";
            ServerUptimeLabel.Content = "";
            System.Windows.Forms.MessageBox.Show($"Server has been shutdown after {timeToDisplay}.", "Server shutdown", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            StartServerButton.IsEnabled = true;
            StopServerButton.IsEnabled = false;
            BGWorker.CancelAsync();
            ConsoleLogTextBox.Clear();
            CommandTextBox.IsEnabled = false;
            SendCommandButton.IsEnabled = false;
            ServerRunning = false;

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

        #endregion

        #region Console Do Work

        private void BGWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string serverFile = Properties.Settings.Default.ServerLocation;
            int ramAmount = Properties.Settings.Default.RamAllocation;
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();

            

            try
            {
                p.StandardInput.WriteLine($"java -Xmx{ramAmount}M -Xms{ramAmount}M -jar \"{serverFile}\" nogui");
                while(!p.HasExited)
                {
                    BGWorker.ReportProgress(0,(string)(p.StandardOutput.ReadLine()));
                    if(BGWorker.CancellationPending)
                    {
                        p.Kill();
                        p.Dispose();
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void BGWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string s = (string)e.UserState;
            ConsoleLogTextBox.AppendText(s);
            ConsoleLogTextBox.AppendText(Environment.NewLine);
            ConsoleLogTextBox.ScrollToEnd();
        }

        private void BGWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }

        private void SendCommandButton_Click(object sender, RoutedEventArgs e)
        {
            p.StandardInput.WriteLine(CommandTextBox.Text);
            CommandTextBox.Clear();
        }

        #endregion

        #region Settings Tab

        private void RamSelectionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RamSelectionSlider.Value < 1024)
                RamAmountLabel.Content = $"MB : {RamSelectionSlider.Value}";
            else
                RamAmountLabel.Content = $"GB : {RamSelectionSlider.Value/1024}";
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(File.Exists(Properties.Settings.Default.ServerLocation))
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

                    Properties.Settings.Default.Save();
                    
                    ImportSettingsText();
                    System.Windows.Forms.MessageBox.Show($"Ram Allocation : {RamSelectionSlider.Value} MB \nServer Location : {ServerLocationTextBox.Text}", "Server Settings Updated");
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
                if(ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
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
                    while((s = rd.ReadLine()) != null)
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
            if(ServerRunning)
            {
                TestRamTimer.Start();
            }
        }

        private void DashboardTab_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ServerRunning)
            {
                TestRamTimer.Stop();
            }
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

            if(backupTimerInt == 0)
            {
                p.StandardInput.WriteLine("/say Server backup starting server may lag.");
                p.StandardInput.WriteLine("/backup start");
                backupTimerInt = 3600;
                DateTime localTime = DateTime.Now;
                LastBackupLabel.Content = localTime;
                Properties.Settings.Default.LastBackup = localTime;
                Properties.Settings.Default.Save();
            }

        }




        #endregion

        private void StartGraphingButton_Click(object sender, RoutedEventArgs e)
        {
            
            if(!ramTimerBool)
            {
                TestRamTimer.Start();
                StartGraphingButton.Content = "Stop Graph Test";
                ramTimerBool = true;
                ServerRunning = true;
            }
            else
            {
                TestRamTimer.Stop();
                StartGraphingButton.Content = "Test Graph";
                ramTimerBool = false;
                RamGraph.Series.Clear();
                ServerRunning = false;
            }
            
        }

        private void TestRamTimerTick(object sender, EventArgs e)
        {
            double[] holderArray = new double[20];
            int temp = rand.Next(0,100);
            int[] xax = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            for(int i = 1; i<20;i++)
            {
                ramGraphArray[i - 1] = ramGraphArray[i];
            }
            ramGraphArray[19] = temp;
            holderArray = ramGraphArray;

            SeriesCollection SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Values = new ChartValues<double>(holderArray),
                    PointGeometrySize = 1
                }
                
            };
            
            
        }

        
    }
}
