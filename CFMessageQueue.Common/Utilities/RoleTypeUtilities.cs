using CFMessageQueue.Enums;

namespace CFMessageQueue.Utilities
{
    /// <summary>
    /// Role type utilities
    /// </summary>
    public static class RoleTypeUtilities
    {
        /// <summary>
        /// Default hub level roles for admin client
        /// </summary>
        public static List<RoleTypes> DefaultAdminHubClientRoleTypes => new List<RoleTypes>()
        {
            RoleTypes.HubAdmin,
            RoleTypes.HubReadMessageHubClients,
            RoleTypes.HubReadMessageHubs,
            RoleTypes.HubReadMessageQueues
        };

        /// <summary>
        /// Default hub level roles for non-admin client
        /// </summary>
        public static List<RoleTypes> DefaultNonAdminHubClientRoleTypes => new List<RoleTypes>()
        {            
            RoleTypes.HubReadMessageHubs,
            RoleTypes.HubReadMessageQueues
        };

        /// <summary>
        /// Default queue level roles for admin client
        /// </summary>
        public static List<RoleTypes> DefaultAdminQueueClientRoleTypes => new List<RoleTypes>()
        {
            RoleTypes.QueueReadQueue,
            RoleTypes.QueueSubscribeQueue,
            RoleTypes.QueueWriteQueue,
        };

        /// <summary>
        /// Default queue level roles for non-admin client
        /// </summary>
        public static List<RoleTypes> DefaultNonAdminQueueClientRoleTypes => new List<RoleTypes>()
        {
            RoleTypes.QueueReadQueue,
            RoleTypes.QueueSubscribeQueue,
            RoleTypes.QueueWriteQueue,             
        };

        /// <summary>
        /// Role types for hub level
        /// </summary>
        public static List<RoleTypes> HubRoleTypes = new List<RoleTypes>()
        {
            RoleTypes.HubAdmin,
            RoleTypes.HubReadMessageHubClients,
            RoleTypes.HubReadMessageHubs,
            RoleTypes.HubReadMessageQueues
        };

        /// <summary>
        /// Role types for queue level
        /// </summary>
        public static List<RoleTypes> QueueRoleTypes = new List<RoleTypes>()
        {
            RoleTypes.QueueReadQueue,
            RoleTypes.QueueSubscribeQueue,
            RoleTypes.QueueWriteQueue
        };
    }
}
