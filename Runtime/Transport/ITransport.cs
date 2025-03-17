using System;
namespace NetPackage.Runtime.Transport
{
    public interface ITransport
    {
        event Action<int> OnClientConnected;
        event Action<int> OnClientDisconnected;
        event Action OnDataReceived;
        void Setup(int port);
        void Start();
        void Connect(string address, int port);
        void Disconnect();
        void Send(byte[] data);
        void SendTo(int id, byte[] data);
        byte[] Receive();
    }
}