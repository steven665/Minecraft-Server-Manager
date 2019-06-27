using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Server_manager_2
{
    class Player
    {
        private bool _OnlineStatus;
        public Player(string _username)
        {
            Username = _username;
            _OnlineStatus = false;
        }
        public Player(string _username, bool b)
        {
            Username = _username;
            _OnlineStatus = b;
        }

        public string Username { get; set; }


        public void SetOnlineStatus(bool b)
        {
            _OnlineStatus = b;
        }

        public string GetOnlineStatus
        {
            get
            {
                if (_OnlineStatus)
                {
                    return "Online";
                }
                else
                {
                    return "Offline";
                }
            }
        }

    }
}
