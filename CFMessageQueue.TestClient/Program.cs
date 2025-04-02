using CFConnectionMessaging.Models;
using CFMessageQueue.Enums;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.Services;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Starting CF Message Queue Test Client");

var adminSecurityKey = "5005db05-35eb-4471-bd05-7883b746b196";
var defaultSecurityKey = "0b38818c-4354-43f5-a750-a24378d2e3a8";

int localPort = 10010;

// Set security key
var securityKey = Guid.Empty.ToString();

var remoteEndpointInfo = new EndpointInfo()
{
    Ip = "192.168.1.45",
    Port = 10000
};
var messageHubClientConnector = new MessageHubClientConnector(remoteEndpointInfo, adminSecurityKey, defaultSecurityKey, localPort);

// Create client
var newClientSecurityKey = "0b38818c-4354-43f5-a750-a24378d2e3a8";
var messageHubClientId = messageHubClientConnector.AddMessageHubClientAsync(newClientSecurityKey).Result;

// Create message queue (Gives full permissions to admin hub client)
var messageQueueId = messageHubClientConnector.AddMessageQueueAsync("Queue 1").Result;

// Configure hub client for specific queue
messageHubClientConnector.ConfigureMessageHubClient(messageHubClientId, messageQueueId,
    new List<RoleTypes>()
    {
        RoleTypes.ClearQueue,
        RoleTypes.ReadQueue,
        RoleTypes.WriteQueue,
        RoleTypes.SubscribeQueue
    }).Wait();

// Get message hubs (Should only be 1 as hub currently only knows about itself)
var messageHubs = messageHubClientConnector.GetMessageHubsAsync().Result;

var messageHub = messageHubs.First();

// Get message queues
var messageQueues = messageHubClientConnector.GetMessageQueuesAsync().Result;

int xxx = 1000;


Console.WriteLine("Terminating CF Message Queue Test Client");