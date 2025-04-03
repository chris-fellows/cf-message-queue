namespace CFMessageQueue.Interfaces
{
    /// <summary>
    /// Serializes queue message conent
    /// </summary>
    public interface IQueueMessageContentSerializer
    {
        byte[] Serialize<TEntity>(TEntity entity);

        TEntity Deserialize<TEntity>(byte[] content);
    }
}
