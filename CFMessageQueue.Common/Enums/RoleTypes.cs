namespace CFMessageQueue.Enums
{
    /// <summary>
    /// Role types. Role refers to whole hub or specific queue
    /// </summary>
    public enum RoleTypes
    {
        Admin,                  // Hub level
        GetMessageHubs,         // Hub level
        GetMessageQueues,       // Hub level
        ClearQueue,             // Queue level
        ReadQueue,              // Queue level
        SubscribeQueue,         // Queue level
        WriteQueue,             // Queue level
    }
}
