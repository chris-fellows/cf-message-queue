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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CFMessageQueue.UI.UserControls
{
    public partial class MessageQueueControl : UserControl
    {
        private IMessageHubClientConnector? _messageHubClientConnector;
        private IMessageQueueClientConnector? _messageQueueClientConnector;

        private List<MessageHubClient> _messageHubClients = new();

        private MessageQueue? _messageQueue;

        public MessageQueueControl()
        {
            InitializeComponent();
        }

        public MessageQueueControl(IMessageHubClientConnector messageHubClientConnector,
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

            lblQueueName.Text = messageQueue.Name;
            lblQueueMaxSize.Text = messageQueue.MaxSize == 0 ? "None" : messageQueue.MaxSize.ToString();
            lblQueueMaxProcessing.Text = messageQueue.MaxConcurrentProcessing == 0 ? "None" : messageQueue.MaxConcurrentProcessing.ToString();

            // Display security items
            dgvClient.Rows.Clear();
            dgvClient.Columns.Clear();
            int columnIndex = dgvClient.Columns.Add("Client", "Client");
            dgvClient.Columns[columnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            columnIndex = dgvClient.Columns.Add("Roles", "Roles");
            dgvClient.Columns[columnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            foreach (var securityItem in messageQueue.SecurityItems)
            {
                dgvClient.Rows.Add(GetSecurityItemRow(securityItem));
            }
        }

        private DataGridViewRow GetSecurityItemRow(SecurityItem securityItem)
        {
            var row = new DataGridViewRow();

            var messsgeHubClient = _messageHubClients.First(c => c.Id == securityItem.MessageHubClientId);

            using (var cell= new DataGridViewTextBoxCell())
            {
                cell.Value = messsgeHubClient.Name;
                row.Cells.Add(cell);
            }

            using (var cell = new DataGridViewTextBoxCell())
            {
                cell.Value = String.Join(",", securityItem.RoleTypes);
                row.Cells.Add(cell);
            }

            return row;
        }
    }
}
