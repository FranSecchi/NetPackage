using System;
namespace NetPackage.Runtime.Transport
{
    public interface ITransport
    {
        event Action<int> OnClientConnected;
        event Action<int> OnClientDisconnected;
        void Setup(int port, bool isServer);
        void Start();
        void Connect(string address, int port);
        void Disconnect();
        void Send(byte[] data);
        byte[] Receive();
    }
}