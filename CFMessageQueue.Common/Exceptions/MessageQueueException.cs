using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Exceptions
{
    public class MessageQueueException : Exception
    {
        //public ResponseErrorCodes? ResponseErrorCode { get; set; }

        public MessageQueueException()
        {
        }

        public MessageQueueException(string message) : base(message)
        {
        }

        public MessageQueueException(string message, Exception innerException) : base(message, innerException)
        {
        }


        public MessageQueueException(string message, params object[] args)
            : base(string.Format(CultureInfo.CurrentCulture, message, args))
        {
        }
    }
}
