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
using System.Threading;


namespace Minecraft_Server_manager_2
{
    class MinecraftServer
    {
        public Process ServerP = new Process();
        bool serverRunning = false;
        int _ramAmount;
        string _serverLocation;
        string _serverFolder;
        ProcessStartInfo PSI;
        string outputLog;
        List<string> outputLog1 = new List<string>();




        public MinecraftServer(string serverLocation, int ramAmount, string serverFolder)
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


        public string GetOutput()
        {
            return outputLog;
        }

        public void StartServer()
        {
            try
            {
                ServerP.StartInfo = PSI;
                ServerP.EnableRaisingEvents = true;
                serverRunning = true;
                ServerP.Start();

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
                ServerP.StandardInput.WriteLine("/stop");
                ServerP.Kill();
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
