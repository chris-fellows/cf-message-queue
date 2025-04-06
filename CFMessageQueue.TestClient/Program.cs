using CFConnectionMessaging.Models;
using CFMessageQueue.Enums;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.Services;
using CFMessageQueue.TestClient;
using CFMessageQueue.TestClient.Models;
using CFMessageQueue.Utilities;
using System.Diagnostics;
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

var id = Guid.NewGuid().ToString();

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Starting CF Message Queue Test Client");

// Run send receive test
//new SendReceiveTest().Run();

// Run producer consumer test
new ProducerConsumerTest().Run(TimeSpan.FromSeconds(60));

Console.WriteLine("Terminating CF Message Queue Test Client");