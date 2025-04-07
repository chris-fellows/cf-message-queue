using CFMessageQueue.Enums;

namespace CFMessageQueue.Models
{
    public class MessageResponse
    {
        /// <summary>
        /// If response then Id of original message
        /// </summary>
        public string MessageId { get; set; } = String.Empty;

        /// <summary>
        /// If response then whether there are more response messages for the same MessageId
        /// </summary>
        public bool IsMore { get; set; } = false;

        /// <summary>
        /// Sequence number for responses for same MessageId (First=0). It can be used to check that all response
        /// messages were received.
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// Response error code
        /// </summary>
        public ResponseErrorCodes? ErrorCode { get; set; }

        /// <summary>
        /// Response error message
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
