using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CFMessageQueue.UI.UserControls
{
    public partial class MessageHubClientControl : UserControl
    {
        private IMessageHubClientConnector? _messageHubClientConnector;
        private IMessageQueueClientConnector? _messageQueueClientConnector;

        private MessageHubClient? _messageHubClient;

        public MessageHubClientControl()
        {
            InitializeComponent();
        }

        public MessageHubClientControl(IMessageHubClientConnector messageHubClientConnector,
                IMessageQueueClientConnector messageQueueClientConnector)
        {
            InitializeComponent();

            _messageHubClientConnector  = messageHubClientConnector;
            _messageQueueClientConnector = messageQueueClientConnector;
        }


        public void ModelToView(MessageHubClient messageHubClient)
        {
            _messageHubClient = messageHubClient;

            txtName.Text = messageHubClient.Name;            
            txtSecurityKey.Text = messageHubClient.SecurityKey;
        }
    }
}
