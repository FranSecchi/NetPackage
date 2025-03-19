using System;

namespace Transport.NetPackage.Runtime.Transport
{
    public enum TransportType
    {
        UDP = 0,
    }
    public interface ITransport
    {
        event Action<int> OnClientConnected;
        event Action<int> OnClientDisconnected;
        event Action OnDataReceived;
        void Setup(int port, bool isServer);
        void Start();
        void Connect(string address);
        void Disconnect();
        void Send(byte[] data);
        void SendTo(int id, byte[] data);
        byte[] Receive();
    }
}