using CFConnectionMessaging.Models;
using CFMessageQueue;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.Services;

namespace CFMessageQueue.UI
{
    public partial class MainForm : Form
    {
        private IMessageHubClientConnector? _messageHubClientConnector;
        private IMessageQueueClientConnector? _messageQueueClientConnector;

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
        }

        private async Task RefreshDisplayAsync()
        {
            var messageQueues = await _messageHubClientConnector.GetMessageQueuesAsync();

            tscbQueue.ComboBox.DataSource = messageQueues;
            tscbQueue.ComboBox.DisplayMember = nameof(MessageQueue.Name);
            tscbQueue.ComboBox.ValueMember = nameof(MessageQueue.Id);
            tscbQueue.ComboBox.SelectedValue = messageQueues[0].Id;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            RefreshDisplayAsync().Wait();
        }

        private void tscbQueue_SelectedIndexChanged(object sender, EventArgs e)
        {
            DisplayQueueMessagesQueue((MessageQueue)tscbQueue.SelectedItem, 100, 1);
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
    }
}
