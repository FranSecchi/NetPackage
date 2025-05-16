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

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting
    }

    public class ServerInfo
    {
        public IPEndPoint EndPoint { get; set; }
        public string ServerName { get; set; }
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string GameMode { get; set; }
        public int Ping { get; set; }
        public Dictionary<string, string> CustomData { get; set; }
    }

    public class ConnectionInfo
    {
        public ConnectionState State { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
        public int Ping { get; set; }
        public int BytesReceived { get; set; }
        public int BytesSent { get; set; }
        public DateTime ConnectedSince { get; set; }
        public float PacketLoss { get; set; }
    }

    public interface ITransport
    {
        [CanBeNull] static event Action<int> OnClientConnected;
        [CanBeNull] static event Action<int> OnClientDisconnected;
        [CanBeNull] static event Action<int> OnDataReceived;
        [CanBeNull] static event Action<ServerInfo> OnLanServerDiscovered;
        [CanBeNull] static event Action<ConnectionInfo> OnConnectionStateChanged;

        void Setup(int port, bool isServer, bool useDebug = false);
        void Start();
        void Stop();
        void Connect(string address);
        void Disconnect();
        void Kick(int id);
        void Send(byte[] data);
        void SendTo(int id, byte[] data);
        byte[] Receive();
        List<ServerInfo> GetDiscoveredServers();
        
        // New methods for connection information
        ConnectionInfo GetConnectionInfo(int clientId);
        ConnectionState GetConnectionState(int clientId);
        
        // Server information methods
        void SetServerInfo(ServerInfo serverInfo);
        ServerInfo GetServerInfo();
        void UpdateServerInfo(Dictionary<string, string> customData);
        
        // Connection quality methods
        void SetBandwidthLimit(int bytesPerSecond);
        
        // Server discovery methods
        void StartServerDiscovery(int discoveryPort = -1);
        void StopServerDiscovery();
        void StopServerBroadcast();
        void BroadcastServerInfo();

        static void TriggerOnClientConnected(int id)
        {
            OnClientConnected?.Invoke(id);
        }

        static void TriggerOnClientDisconnected(int id)
        {
            OnClientDisconnected?.Invoke(id);
        }

        static void TriggerOnDataReceived(int id)
        {
            OnDataReceived?.Invoke(id);
        }

        static void TriggerOnLanServerDetected(ServerInfo serverInfo)
        {
            OnLanServerDiscovered?.Invoke(serverInfo);
        }

        static void TriggerOnConnectionStateChanged(ConnectionInfo connectionInfo)
        {
            OnConnectionStateChanged?.Invoke(connectionInfo);
        }
    }
}