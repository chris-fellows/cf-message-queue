using CFConnectionMessaging;
using CFConnectionMessaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub
{
    public class MessageClientConnection
    {
        private ConnectionTcp _connectionTcp = new ConnectionTcp();


        public delegate void ConnectionMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo);
        public event ConnectionMessageReceived? OnConnectionMessageReceived;

        public MessageClientConnection()
        {
            _connectionTcp.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                if (IsResponseMessage(connectionMessage))     // Inside Send... method waiting for response
                {
                    // No current requests do this
                }
                else
                {
                    if (OnConnectionMessageReceived != null)
                    {
                        OnConnectionMessageReceived(connectionMessage, messageReceivedInfo);
                    }
                }
            };
        }

        private void _connectionTcp_OnConnectionMessageReceived(CFConnectionMessaging.Models.ConnectionMessage connectionMessage, CFConnectionMessaging.Models.MessageReceivedInfo messageReceivedInfo)
        {
            throw new NotImplementedException();
        }

        private bool IsResponseMessage(ConnectionMessage connectionMessage)
        {
            return false;
        }
    }
}
