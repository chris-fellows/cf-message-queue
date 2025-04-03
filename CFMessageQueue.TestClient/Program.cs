using CFConnectionMessaging.Models;
using CFMessageQueue.Enums;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.Services;
using CFMessageQueue.TestClient;
using CFMessageQueue.TestClient.Models;

var id = Guid.NewGuid().ToString();

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Starting CF Message Queue Test Client");

var configurer = new Configurer();

// Create message queue
var messageQueueId = configurer.CreateMessageQueueAsync(SystemConfig.HubEndpointInfo, SystemConfig.QueueName1).Result;

// Create message hub clients
var messageHubClientId1 = configurer.CreateMessageHubClientAsync(SystemConfig.HubEndpointInfo, messageQueueId, SystemConfig.Client1SecurityKey).Result;
var messageHubClientId2 = configurer.CreateMessageHubClientAsync(SystemConfig.HubEndpointInfo, messageQueueId, SystemConfig.Client2SecurityKey).Result;

// Get message hubs
var messageHubClientConnector = new MessageHubClientConnector(SystemConfig.HubEndpointInfo, SystemConfig.AdminSecurityKey, SystemConfig.Client1SecurityKey, 10010);

var messageQueues = messageHubClientConnector.GetMessageQueuesAsync();

int xxxx = 1000;

//var messageHubs = messageHubClientConnector.GetMessageHubsAsync().Result;
//var messageHub = messageHubs.FirstOrDefault();



//// Get message hubs (Should only be 1 as hub currently only knows about itself)
//var messageHubs = messageHubClientConnector.GetMessageHubsAsync().Result;

//var messageHub = messageHubs.First();

//// Get message queues
//var messageQueues = messageHubClientConnector.GetMessageQueuesAsync().Result;

//int xxx = 1000;


Console.WriteLine("Terminating CF Message Queue Test Client");