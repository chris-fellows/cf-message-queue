using System.Net;
using System.Net.Sockets;

namespace CFMessageQueue.Utilities
{ 
    /// <summary>
    /// Network utilities
    /// </summary>
    public static class NetworkUtilities
    {
        public static List<string> GetLocalIPV4Addresses()
        {
            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddresses = hostEntry.AddressList.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToList();

            return ipAddresses.Select(a => a.ToString()).ToList();
        }

        /// <summary>
        /// Gets a free local port within port range and not used port
        /// </summary>
        /// <param name="minPort"></param>
        /// <param name="maxPort"></param>
        /// <returns></returns>
        public static int GetFreeLocalPort(int minPort, int maxPort,
                                        List<int> usedPorts)
        {
            var port = minPort - 1;

            do
            {
                port++;
                if (!usedPorts.Contains(port) && IsLocalPortFree(port)) return port;
            } while (port < maxPort);

            return 0;   // No free ports
        }

        /// <summary>
        /// Returns if port is free
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool IsLocalPortFree(int port)
        {
            try
            {
                using (var tcpLisener = new TcpListener(System.Net.IPAddress.Any, port))
                {
                    tcpLisener.Start();
                    tcpLisener.Stop();
                }
                return true;
            }
            catch { };

            return false;
        }
    }
}
