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
using System.Threading;


namespace Minecraft_Server_Manager_2._0
{
    class MinecraftServer
    {
        public Process ServerP = new Process();
        bool serverRunning = false;
        int _ramAmount;
        string _serverLocation;
        string _serverFolder;
        ProcessStartInfo PSI;
        List<string> outputLog = new List<string>();
        List<string> outputLog1 = new List<string>();
        int pulling = 0;
        
        
        

        public MinecraftServer(string serverLocation, int ramAmount,string serverFolder)
        {
            if (File.Exists(serverLocation))
            {
                _serverLocation = serverLocation;
                _serverFolder = serverFolder;
                _ramAmount = ramAmount;
            }

            //"java -Xmx{_ramAmount}M -Xms{_ramAmount}M -jar \"{_serverLocation}\" nogui"
            PSI = new ProcessStartInfo("java", $"-Xmx{_ramAmount}M -Xms{_ramAmount}M -jar  {System.IO.Path.GetFileName(_serverLocation)} nogui")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = _serverFolder,

            };

            
        }

        private void DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (pulling == 0)
            {
                outputLog.Add(e.Data);
            }
            else
            {
                outputLog1.Add(e.Data);
            }
        }

        public List<string> GetOutput()
        {
            if (pulling == 0)
            {
                pulling = 1;
                outputLog.Clear();
                return outputLog;
                
            }
            else
            {
                pulling = 0;
                outputLog1.Clear();
                return outputLog1;
                
            }
        }

        public void StartServer()
        {
            try
            {

                ServerP.StartInfo = PSI;
                ServerP.EnableRaisingEvents = true;
                ServerP.OutputDataReceived += new DataReceivedEventHandler(DataReceived);
                serverRunning = true;
                ServerP.Start();
                ServerP.BeginOutputReadLine();

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                throw;
            }

        }

        public void StopServer()
        {
            try
            {
                serverRunning = false;
                ServerP.Kill();
                System.Windows.Forms.MessageBox.Show("Server has exited");

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public bool ServerStatus()
        {
            return serverRunning;
        }
        
        public void SendCommand(string c)
        {
            ServerP.StandardInput.WriteLine(c);
        }
        
        public void GetMembers()
        {

        }

        public void GetMembersOnline()
        {

        }

        public void PreformServerAction(string action, string user)
        {

        }
        
        

        

        
    }
}
