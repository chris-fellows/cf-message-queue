using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;

namespace CFMessageQueue.UI.UserControls
{
    public partial class QueueMessagesControl : UserControl
    {
        private IMessageHubClientConnector? _messageHubClientConnector;
        private IMessageQueueClientConnector? _messageQueueClientConnector;

        private int _page = 1;
        private int _pageItems = 50;
        private MessageQueue? _messageQueue;

        private List<MessageHubClient> _messageHubClients = new();

        public QueueMessagesControl()
        {
            InitializeComponent();
        }

        public QueueMessagesControl(IMessageHubClientConnector messageHubClientConnector,
                IMessageQueueClientConnector messageQueueClientConnector)
        {
            InitializeComponent();

            _messageHubClientConnector = messageHubClientConnector;
            _messageQueueClientConnector = messageQueueClientConnector;
        }

        public void ModelToView(MessageQueue messageQueue)
        {
            _messageQueue = messageQueue;           

            if (!_messageHubClients.Any())
            {
                _messageHubClients = _messageHubClientConnector.GetMessageHubClientsAsync().Result;
            }

            Page = 1;
        }

        private int Page
        {
            get { return _page; }
            set
            {
                _page = value;

                tslPage.Text = $"Page {_page}";
                tsbNextPage.Enabled = true;
                tsbPrevPage.Enabled = _page > 1;

                DisplayQueueMessagesQueueAsync(_messageQueue, _pageItems, _page);
            }
        }

        /// <summary>
        /// Display queue messages for page
        /// </summary>
        /// <param name="messageQueue"></param>
        /// <param name="pageItems"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        private async Task DisplayQueueMessagesQueueAsync(MessageQueue messageQueue, int pageItems, int page)
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
            dgvQueueMessage.Columns[columnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            columnIndex = dgvQueueMessage.Columns.Add("Type", "Type");
            dgvQueueMessage.Columns[columnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            columnIndex = dgvQueueMessage.Columns.Add("Created", "Created");
            dgvQueueMessage.Columns[columnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            columnIndex = dgvQueueMessage.Columns.Add("Name", "Name");
            dgvQueueMessage.Columns[columnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            columnIndex = dgvQueueMessage.Columns.Add("Expiry (Secs)", "Expiry (Secs)");
            dgvQueueMessage.Columns[columnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            columnIndex = dgvQueueMessage.Columns.Add("Client", "Client");
            dgvQueueMessage.Columns[columnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            columnIndex = dgvQueueMessage.Columns.Add("Content", "Content");
            dgvQueueMessage.Columns[columnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            foreach (var queueMessage in queueMessages)
            {                
                dgvQueueMessage.Rows.Add(CreateQueueMessageRow(queueMessage));
            }

            tsbNextPage.Enabled = queueMessages.Any();
        }

        private DataGridViewRow CreateQueueMessageRow(QueueMessage queueMessage)
        {
            var row = new DataGridViewRow();

            var messageHubClient = _messageHubClients.First(c => c.Id == queueMessage.SenderMessageHubClientId);

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
                cell.Value = queueMessage.Name;
                row.Cells.Add(cell);
            }

            using (var cell = new DataGridViewTextBoxCell())
            {
                cell.Value = queueMessage.ExpirySeconds.ToString();
                row.Cells.Add(cell);
            }

            using (var cell = new DataGridViewTextBoxCell())
            {
                cell.Value = messageHubClient.Name;
                row.Cells.Add(cell);
            }

            using (var cell = new DataGridViewTextBoxCell())
            {
                if (queueMessage.Content == null)
                {
                    cell.Value = "None";
                }
                else
                {
                    cell.Value = queueMessage.Content.GetType().AssemblyQualifiedName;   
                }
                row.Cells.Add(cell);
            }

            return row;
        }

        private void tsbNextPage_Click(object sender, EventArgs e)
        {
            Page++;
        }

        private void tsbPrevPage_Click(object sender, EventArgs e)
        {
            Page--;
        }
    }
}
