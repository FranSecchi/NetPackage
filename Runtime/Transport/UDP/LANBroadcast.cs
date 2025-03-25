using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public class LANBroadcast
    {
        private UdpClient _udpBroadcaster;
        private IPEndPoint _broadcastEP;
        private const int DiscoveryPort = 9050;
        private bool _isBroadcasting;
        private Thread _broadcastThread;

        public void StartBroadcast()
        {
            _udpBroadcaster = new UdpClient();
            _udpBroadcaster.EnableBroadcast = true;
            _broadcastEP = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
            _isBroadcasting = true;
            _broadcastThread = new Thread(BroadcastPresence)
            {
                IsBackground = true
            };
            _broadcastThread.Start();
        }

        private void BroadcastPresence()
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes("ServerAvailable");

            while (_isBroadcasting)
            {
                _udpBroadcaster.Send(data, data.Length, _broadcastEP);
                Thread.Sleep(2000); // Broadcast every 2 seconds
            }
        }

        public void StopBroadcast()
        {
            _isBroadcasting = false;
            _broadcastThread?.Join();
            _udpBroadcaster?.Close();
        }
    }
}
