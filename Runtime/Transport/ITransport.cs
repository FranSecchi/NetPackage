using System;
using System.Collections.Generic;
using System.Net;
using JetBrains.Annotations;

namespace Transport.NetPackage.Runtime.Transport
{
    public enum TransportType
    {
        UDP = 0,
    }
    public interface ITransport
    {
        [CanBeNull] static event Action<int> OnClientConnected;
        [CanBeNull] static event Action<int> OnClientDisconnected;
        [CanBeNull] static event Action OnDataReceived;
        void Setup(int port, bool isServer, bool isBroadcast = false);
        void Start();
        void Connect(string address);
        void Disconnect();
        void Kick(int id);
        void Send(byte[] data);
        void SendTo(int id, byte[] data);
        byte[] Receive();
        List<IPEndPoint> GetDiscoveredServers();

        static void TriggerOnClientConnected(int id)
        {
            OnClientConnected?.Invoke(id);
        }

        static void TriggerOnClientDisconnected(int id)
        {
            OnClientDisconnected?.Invoke(id);
        }

        static void TriggerOnDataReceived()
        {
            OnDataReceived?.Invoke();
        }
    }
}