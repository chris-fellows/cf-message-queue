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
        public byte[] Serialize<TEntity>(TEntity entity)
        {
            return Encoding.UTF8.GetBytes(JsonUtilities.SerializeToString(entity, JsonUtilities.DefaultJsonSerializerOptions));            
        }

        public TEntity Deserialize<TEntity>(byte[] content)
        {
            return JsonUtilities.DeserializeFromString<TEntity>(Encoding.UTF8.GetString(content), JsonUtilities.DefaultJsonSerializerOptions);
        }
    }
}
