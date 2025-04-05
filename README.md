# cf-message-queue

Message queuing mechanism. System comprises of a number of hubs that contain message queues. Clients
send messages to specific queues and other clients can access those messages.

#Message Queue Hub
Message Queue Hub hosts one or more message queues. Clients communicate via TCP.

#Hub Client
Hub clients communicate the the Message Queue Hub via TCP. The client passes a security key to indicate
which client that it is.

Hub clients use the following classes to communicate with the hub:
- MessageHubClientConnector : Hub level actions. E.g. Create queue, get queue list.
- MessageQueueClientConnector : Queue levelactions. E.g. Add message, get next message.

#Producer Messages
The sequence of actions is as follows:
- Create instance of MessageQueueClientConnector class.
- Call MessageQueueClientConnector.SendAsync method.

#Consuming Messages
Clients request a message from the hub, process the message (or not process it) and then inform the
hub whether the message was processed. If the client does not inform the hub then the message will
be available to be processed be any client.

The sequence of actions is as follows:
- Create instance of MessageHubClientConnector class.
- Call MessageHubClientConnector.GetMessageQueuesAsync method to get queue list.
- Create instance of MessageQueueClientConnector class.
- Call MessageQueueClientConnector.SetMessageQueue method to set the current message queue.
- Call MessageQueueClientConnector.GetNextAsync method.
- Process message.
- Call MessageQueueClientConnector.SetProcessed(QueueMessage.Id, IsProcessed)

#Registering New Client
The sequence of actions is as follows:
- Create instance of MessageHubClientConnector class passing the Admin security key to the constructor.
- Create new security key. E.g. Call MessageHubClientConnector.CreateRandomSecurityKey method.
- Call MessageHubClientConnector.AddMessageHubClientAsync method and pass the security key.
- Store the security key for future use.

#Registering New Queue
When a queue is created then each client must be assigned the allowed roles for accessing the queue.

The sequence of actions is as follows:
- Create instance of MessageHubClientConnector class passing the Admin security key to the constructor.
- Call MessageHubClientConnector.AddMessageQueueAsync method and pass the queue name.
- Call AddMessageQueueAsync.ConfigureMessageHubClientAsync method to set the roles for each client.

#Notifications
Clients can subscribe to the following notifications from the Message Queue Hub:
- Messages added to queue (E.g. After previously empty).
- Queue cleared.
- Current queue size (Sent periodically).

The sequence of actions is:
- Create instance of MessageQueueClientConnector class.
- Call MessageQueueClientConnector.SubscribeAsync method and pass event handler.
- Perform processing.
- Call MessageQueueClientConnector.UnsubscribeAsync method to unsubscribe from notifications.

#Security
Clients must pass a security key when communicating with Message Queue Hub. Each security key is linked to 
a set of roles to indicate what actions are allowed for the client.

An Admin role allows the client to peform functions such as creating/deleting queues & registering new
clients.




