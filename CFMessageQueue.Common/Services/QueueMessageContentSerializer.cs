using CFMessageQueue.Interfaces;
using CFMessageQueue.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Services
{
    public class QueueMessageContentSerializer : IQueueMessageContentSerializer
    {
        public byte[] Serialize(object entity, Type entityType)
        {
            return Encoding.UTF8.GetBytes(JsonUtilities.SerializeToString(entity, entityType, JsonUtilities.DefaultJsonSerializerOptions));            
        }

        public object Deserialize(byte[] content, Type entityType)
        {
            return JsonUtilities.DeserializeFromString(Encoding.UTF8.GetString(content), entityType, JsonUtilities.DefaultJsonSerializerOptions);
        }
    }
}
