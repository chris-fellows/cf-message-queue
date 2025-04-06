using CFConnectionMessaging.Models;

namespace CFMessageQueue.UI
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var securityKey = "5005db05-35eb-4471-bd05-7883b746b196";       // Admin security key
            var remoteEndpointInfo = new EndpointInfo() { Ip = "192.168.1.45", Port = 10000 };            

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(remoteEndpointInfo, securityKey));
        }
    }
}