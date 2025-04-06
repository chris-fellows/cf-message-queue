using CFConnectionMessaging.Models;
using CFMessageQueue;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.Services;
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
            Unknown               
        }

        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(EndpointInfo remoteEndpoint, string securityKey)
        {
            InitializeComponent();

            this.Text = $"CF Message Queue - [{remoteEndpoint.Ip}:{remoteEndpoint.Port}]";

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

            return MyNodeTypes.Unknown;
        }

        private async Task RefreshTreeView()
        {            
            tvwNodes.Nodes.Clear();

            //var messageHubs = await _messageHubClientConnector.GetMessageHubsAsync();

            // Display queues
            var messageQueues = await _messageHubClientConnector.GetMessageQueuesAsync();


            var nodeQueues = tvwNodes.Nodes.Add("Queues", "Queues");
            foreach (var messageQueue in messageQueues)
            {
                var nodeQueue = nodeQueues.Nodes.Add($"Queue.{messageQueue.Id}", messageQueue.Name);
                nodeQueue.Tag = messageQueue;
            }
            nodeQueues.Expand();

            // Display clients
            var messageHubClients = await _messageHubClientConnector.GetMessageHubClientsAsync();

            var nodeClients = tvwNodes.Nodes.Add("Clients", "Clients");           
            foreach(var messageHubClient in messageHubClients)
            {
                var nodeClient = nodeClients.Nodes.Add($"Client.{messageHubClient.Id}", messageHubClient.Id);
                nodeClient.Tag = messageHubClient;
            }
            nodeClients.Expand();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            RefreshTreeView().Wait();
        }

        private void tscbQueue_SelectedIndexChanged(object sender, EventArgs e)
        {
            //DisplayQueueMessagesQueue((MessageQueue)tscbQueue.SelectedItem, 100, 1);
        }

        /// <summary>
        /// Display queue messages for page
        /// </summary>
        /// <param name="messageQueue"></param>
        /// <param name="pageItems"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        private async Task DisplayQueueMessagesQueue(MessageQueue messageQueue, int pageItems, int page)
        {
            // Set message queue
            if (_messageQueueClientConnector != messageQueue)
            {
                _messageQueueClientConnector.MessageQueue = messageQueue;
            }

            // Get messages
            var queueMessages = await _messageQueueClientConnector.GetQueueMessages(pageItems, page);

            // Display messages
            dgvQueueMessage.Rows.Clear();
            dgvQueueMessage.Columns.Clear();
            int columnIndex = dgvQueueMessage.Columns.Add("Id", "Id");
            columnIndex = dgvQueueMessage.Columns.Add("Type", "Type");
            columnIndex = dgvQueueMessage.Columns.Add("Created", "Created");
            columnIndex = dgvQueueMessage.Columns.Add("Expiry", "Expiry");
            columnIndex = dgvQueueMessage.Columns.Add("Client Id", "Client Id");

            foreach (var queueMessage in queueMessages)
            {
                dgvQueueMessage.Rows.Add(CreateQueueMessageRow(queueMessage));
            }
        }

        private DataGridViewRow CreateQueueMessageRow(QueueMessage queueMessage)
        {
            var row = new DataGridViewRow();

            using (var cell = new DataGridViewTextBoxCell())
            {
                cell.Value = queueMessage.Id;
                row.Cells.Add(cell);
            }

            using (var cell = new DataGridViewTextBoxCell())
            {
                cell.Value = queueMessage.TypeId;
                row.Cells.Add(cell);
            }

            using (var cell = new DataGridViewTextBoxCell())
            {
                cell.Value = queueMessage.CreatedDateTime.ToString();
                row.Cells.Add(cell);
            }

            using (var cell = new DataGridViewTextBoxCell())
            {
                cell.Value = queueMessage.ExpirySeconds.ToString();
                row.Cells.Add(cell);
            }

            using (var cell = new DataGridViewTextBoxCell())
            {
                cell.Value = queueMessage.SenderMessageHubClientId.ToString();
                row.Cells.Add(cell);
            }

            return row;
        }

        private void tvwNodes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            switch(GetNodeType(e.Node))
            {
                case MyNodeTypes.MessageQueue:
                    var messageQueue = (MessageQueue)e.Node.Tag;

                    DisplayQueueMessagesQueue(messageQueue, 100, 1);
                    break;
            }
        }
    }
}
