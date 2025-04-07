using CFConnectionMessaging.Models;
using CFMessageQueue;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.Services;
using CFMessageQueue.UI.UserControls;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CFMessageQueue.UI
{
    public partial class MainForm : Form
    {
        private IMessageHubClientConnector? _messageHubClientConnector;
        private IMessageQueueClientConnector? _messageQueueClientConnector;

        private enum MyNodeTypes
        {
            MessageHubClient,
            MessageQueue,
            MessageQueueMessages,
            Unknown               
        }

        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(EndpointInfo remoteEndpoint, string securityKey)
        {
            InitializeComponent();

            this.Text = $"CF Message Queue - [Hub {remoteEndpoint.Ip}:{remoteEndpoint.Port}]";

            _messageHubClientConnector = new MessageHubClientConnector(remoteEndpoint, securityKey, 10080);
            _messageQueueClientConnector = new MessageQueueClientConnector(securityKey, 10081);

            /*
            _messageQueueClientConnector.SubscribeAsync((eventName, queueSize) =>
            {

            }, TimeSpan.FromSeconds(30));
            */

            //var messageHubClients = _messageHubClientConnector.GetMessageHubClientsAsync().Result;
            //int xxx = 1000;
        }

        private MyNodeTypes GetNodeType(TreeNode treeNode)
        {
            if (treeNode.Name.StartsWith("Client.")) return MyNodeTypes.MessageHubClient;
            if (treeNode.Name.StartsWith("Queue.")) return MyNodeTypes.MessageQueue;
            if (treeNode.Name.StartsWith("QueueMessages.")) return MyNodeTypes.MessageQueueMessages;

            return MyNodeTypes.Unknown;
        }

        private async Task RefreshTreeView()
        {            
            tvwNodes.Nodes.Clear();

            //var messageHubs = await _messageHubClientConnector.GetMessageHubsAsync();

            // Display queues
            var messageQueues = await _messageHubClientConnector.GetMessageQueuesAsync();


            var nodeQueues = tvwNodes.Nodes.Add("Queues", "Queues");
            foreach (var messageQueue in messageQueues.OrderBy(q => q.Name))
            {
                var nodeQueue = nodeQueues.Nodes.Add($"Queue.{messageQueue.Id}", messageQueue.Name);
                nodeQueue.Tag = messageQueue;

                var nodeQueueMessages = nodeQueue.Nodes.Add($"QueueMessages.{messageQueue.Id}", "Messages");
                nodeQueueMessages.Tag = messageQueue;

            }
            nodeQueues.ExpandAll();

            // Display clients
            var messageHubClients = await _messageHubClientConnector.GetMessageHubClientsAsync();

            var nodeClients = tvwNodes.Nodes.Add("Clients", "Clients");           
            foreach(var messageHubClient in messageHubClients.OrderBy(c => c.Name))
            {
                var nodeClient = nodeClients.Nodes.Add($"Client.{messageHubClient.Id}", messageHubClient.Name);
                nodeClient.Tag = messageHubClient;
            }
            nodeClients.ExpandAll();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            RefreshTreeView().Wait();
        }      
     
        private void tvwNodes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            switch(GetNodeType(e.Node))
            {
                case MyNodeTypes.MessageHubClient:
                    DisplayControl((MessageHubClient)e.Node.Tag);
                    break;

                case MyNodeTypes.MessageQueue:
                    DisplayControl((MessageQueue)e.Node.Tag);
                    break;

                case MyNodeTypes.MessageQueueMessages:
                    DisplayControl((MessageQueue)e.Node.Tag, 0);
                    break;
            }
        }

        /// <summary>
        /// Displays control for MessageHubClient
        /// </summary>
        /// <param name="messageHubClient"></param>
        private void DisplayControl(MessageHubClient messageHubClient)
        {
            splitContainer1.Panel2.Controls.Clear();

            var control = new MessageHubClientControl(_messageHubClientConnector, _messageQueueClientConnector);            
            control.Dock = DockStyle.Fill;
            splitContainer1.Panel2.Controls.Add(control);

            control.ModelToView(messageHubClient);
        }

        /// <summary>
        /// Displays control for MessageQueue
        /// </summary>
        /// <param name="messageQueue"></param>
        private void DisplayControl(MessageQueue messageQueue)
        {
            splitContainer1.Panel2.Controls.Clear();

            var control = new MessageQueueControl(_messageHubClientConnector, _messageQueueClientConnector);            
            control.Dock = DockStyle.Fill;
            splitContainer1.Panel2.Controls.Add(control);

            control.ModelToView(messageQueue);
        }

        /// <summary>
        /// Displays control for MessageQueue messages
        /// </summary>
        /// <param name="messageQueue"></param>
        private void DisplayControl(MessageQueue messageQueue, int xxx)
        {
            splitContainer1.Panel2.Controls.Clear();

            var control = new QueueMessagesControl(_messageHubClientConnector, _messageQueueClientConnector);
            control.Dock = DockStyle.Fill;
            splitContainer1.Panel2.Controls.Add(control);

            control.ModelToView(messageQueue);
        }
    }
}
