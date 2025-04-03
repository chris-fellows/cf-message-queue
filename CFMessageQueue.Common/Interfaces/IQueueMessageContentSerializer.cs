namespace CFMessageQueue.Interfaces
{
    /// <summary>
    /// Serializes queue message conent
    /// </summary>
    public interface IQueueMessageContentSerializer
    {
        byte[] Serialize(object entity, Type entityType);

        object Deserialize(byte[] content, Type entityType);
    }
}
