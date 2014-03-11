using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoggleServer;
using System.Threading;
using BoggleClient;

namespace BoggleClient
{
    class Launcher
    {
        /// <summary>
        /// Launches a chat server and two chat clients
        /// </summary>
        static void Main(string[] args)
        {
            new BoggleServer.BoggleServer(2000, 20, "..\\..\\..\\dictionary", null);
            new Thread(() => BoggleClientView.Main()).Start();
            new Thread(() => BoggleClientView.Main()).Start();
        }
    }
}
