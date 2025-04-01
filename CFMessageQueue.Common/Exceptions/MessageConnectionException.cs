using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Exceptions
{
    public class MessageConnectionException : Exception
    {
        //public ResponseErrorCodes? ResponseErrorCode { get; set; }

        public MessageConnectionException()
        {
        }

        public MessageConnectionException(string message) : base(message)
        {
        }

        public MessageConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }


        public MessageConnectionException(string message, params object[] args)
            : base(string.Format(CultureInfo.CurrentCulture, message, args))
        {
        }
    }
}
