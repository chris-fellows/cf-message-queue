using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    /// <summary>
    /// Test object for use as queue message content
    /// </summary>
    public class TestObject
    {
        public string Id { get; set; } = String.Empty;

        public bool BooleanValue { get; set; }

        public Int32 Int32Value { get; set; }

        public Int64 Int64Value { get; set; }

        public Decimal DecimalValue { get; set; }

        public DateTime DateTimeValue { get; set; }
    }
}
