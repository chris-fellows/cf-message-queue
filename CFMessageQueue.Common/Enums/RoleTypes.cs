namespace CFMessageQueue.Enums
{
    /// <summary>
    /// Role types. Role type is either hub level or queue level.
    /// </summary>
    /// <remarks>Please keep RoleTypeUtilities class updated with changes</remarks>
    public enum RoleTypes
    {
        // Hub level roles
        HubAdmin,                   // Includes clear/delete queue
        HubReadMessageHubClients,
        HubReadMessageHubs,      
        HubReadMessageQueues,    

        // Queue level roles        
        QueueReadQueue,
        QueueSubscribeQueue,
        QueueWriteQueue,
    }
}
