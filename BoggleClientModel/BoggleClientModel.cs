using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomNetworking;
using System.Net.Sockets;

namespace BoggleClient
{
    public class BoggleClientModel
    {
        private StringSocket ss;
        //indicator of whether the client is connected
        private bool isConnected;
        
        public bool IsConnected
        {
            get { return isConnected; }
            set { isConnected = value; }
        }

        // Register for this event to be motified when a line of text arrives.
        public event Action<String> IncomingLineEvent;

        public BoggleClientModel()
        {
            ss = null;
        }

        //make sure the client isn't already connected
        //then connect
        public void Connect(int port, string hostName)
        {
            if (ss == null)
            {
                TcpClient client = new TcpClient(hostName, port);
                ss = new StringSocket(client.Client, UTF8Encoding.Default);
                IsConnected = true;
            }
        }

        //send the play message to the server
        public void Play(string name)
        {
            ss.BeginSend("PLAY " + name + "\n", (e, p) => { }, null);
            ss.BeginReceive(ReceivedMessages, null);
        }

        /// <summary>
        /// Send a line of text to the server.
        /// </summary>
        /// <param name="line"></param>
        public void SendMessage(String line)
        {
            if (ss != null)
            {
                ss.BeginSend("WORD " + line + "\n", (e, p) => { }, null);
            }
        }

        /// <summary>
        /// Deal with an arriving line of text.
        /// </summary>
        public void ReceivedMessages(String s, Exception e, object p)
        {
            if (IncomingLineEvent != null)
            {
                IncomingLineEvent(s);
            }
            if(ss != null)
                ss.BeginReceive(ReceivedMessages, null);
        }

        //Disconnects the client, close the socket
        public void Disconnect()
        {
            if (ss != null)
            {
                ss.Close();
                IsConnected = false;
                ss = null;
            }
            
        }
    }
}
