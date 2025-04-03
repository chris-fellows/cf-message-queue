using System.Net;

namespace CFMessageQueue.Common
{
    public static class NetworkUtilities
    {
        public static List<string> GetLocalIPV4Addresses()
        {
            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddresses = hostEntry.AddressList.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToList();

            return ipAddresses.Select(a => a.ToString()).ToList();
        }
    }
}
