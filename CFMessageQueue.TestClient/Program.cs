using CFConnectionMessaging.Models;
using CFMessageQueue.Enums;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.Services;
using CFMessageQueue.TestClient;
using CFMessageQueue.TestClient.Models;
using CFMessageQueue.Utilities;
using System.Net;

//var oldObject = new MessageHubClient()
//{
//    Id = Guid.NewGuid().ToString()
//};

//// Serialize object
//var serializedObject = JsonUtilities.SerializeToString(oldObject, oldObject.GetType(), JsonUtilities.DefaultJsonSerializerOptions);
//var myType = oldObject.GetType();
//var objectTypeName = oldObject.GetType().AssemblyQualifiedName;

//// Deserialize object from type name and serialized string
//var newObjectType = Type.GetType(objectTypeName);
//var newObject = JsonUtilities.DeserializeFromString(serializedObject, newObjectType, JsonUtilities.DefaultJsonSerializerOptions);

//int xxx = 1000;

//var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
//var ipAddresses = hostEntry.AddressList.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToList();
//var ipAddress = hostEntry.AddressList[0].ToString();

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Starting CF Message Queue Test Client");

var configurer = new Configurer();

// Create message queue
var messageQueueId = configurer.CreateMessageQueueAsync(SystemConfig.HubEndpointInfo, SystemConfig.QueueName1, SystemConfig.HubClientLocalPort).Result;

// Create message hub clients
var messageHubClientId1 = configurer.CreateMessageHubClientAsync(SystemConfig.HubEndpointInfo, messageQueueId, SystemConfig.Client1SecurityKey, SystemConfig.HubClientLocalPort).Result;
var messageHubClientId2 = configurer.CreateMessageHubClientAsync(SystemConfig.HubEndpointInfo, messageQueueId, SystemConfig.Client2SecurityKey, SystemConfig.HubClientLocalPort).Result;

// Get message hubs
var messageHubClientConnector = new MessageHubClientConnector(SystemConfig.HubEndpointInfo, SystemConfig.AdminSecurityKey, SystemConfig.Client1SecurityKey, SystemConfig.HubClientLocalPort);

var messageQueues = messageHubClientConnector.GetMessageQueuesAsync().Result;

// Test sending message to queue
var messageQueue = messageQueues.First();
var messageQueueTest = new MessageQueueTest();
messageQueueTest.SendQueueMessage(messageQueue, SystemConfig.Client1SecurityKey, SystemConfig.QueueLocalPort).Wait();

// Test getting message
var queueMessage = messageQueueTest.GetNextQueueMessage(messageQueue, SystemConfig.Client1SecurityKey, SystemConfig.QueueLocalPort).Result;

Console.WriteLine("Terminating CF Message Queue Test Client");