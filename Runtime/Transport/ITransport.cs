using System;
using System.Collections.Generic;
using System.Net;
using JetBrains.Annotations;

namespace NetPackage.Transport
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
        public int Id { get; set; }
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
        /// <summary>
        /// Event triggered when a new client connects to the server
        /// </summary>
        [CanBeNull] static event Action<int> OnClientConnected;
        
        /// <summary>
        /// Event triggered when a client disconnects from the server
        /// </summary>
        [CanBeNull] static event Action<int> OnClientDisconnected;
        
        /// <summary>
        /// Event triggered when data is received from a client
        /// </summary>
        [CanBeNull] static event Action<int> OnDataReceived;
        
        /// <summary>
        /// Event triggered when a new LAN server is discovered
        /// </summary>
        [CanBeNull] static event Action<ServerInfo> OnLanServerUpdate;
        
        /// <summary>
        /// Event triggered when the connection state changes
        /// </summary>
        [CanBeNull] static event Action<ConnectionInfo> OnConnectionStateChanged;

        /// <summary>
        /// Initializes the transport with the specified port and mode
        /// </summary>
        /// <param name="port">The port to use for communication</param>
        /// <param name="isServer">Whether this instance should act as a server</param>
        /// <param name="maxPlayers">Number of maximum connections on server</param>
        /// <param name="useDebug">Whether to enable debug logging</param>
        void Setup(int port, bool isServer, int maxPlayers = 10, bool useDebug = false);
        void Setup(int port, ServerInfo serverInfo, bool useDebug = false);

        /// <summary>
        /// Starts the transport service
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the transport service
        /// </summary>
        void Stop();

        /// <summary>
        /// Connects to a server at the specified address
        /// </summary>
        /// <param name="address">The server address to connect to</param>
        void Connect(string address);

        /// <summary>
        /// Disconnects from the current connection
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Kicks a client from the server
        /// </summary>
        /// <param name="id">The ID of the client to kick</param>
        void Kick(int id);

        /// <summary>
        /// Sends data to all connected clients (server) or to the server (client)
        /// </summary>
        /// <param name="data">The data to send</param>
        void Send(byte[] data);

        /// <summary>
        /// Sends data to a specific client
        /// </summary>
        /// <param name="id">The ID of the client to send to</param>
        /// <param name="data">The data to send</param>
        void SendTo(int id, byte[] data);

        /// <summary>
        /// Receives data from the network
        /// </summary>
        /// <returns>The received data</returns>
        byte[] Receive();

        /// <summary>
        /// Gets a list of discovered servers
        /// </summary>
        /// <returns>A list of server information</returns>
        List<ServerInfo> GetDiscoveredServers();
        
        /// <summary>
        /// Gets detailed connection information for a specific client
        /// </summary>
        /// <param name="clientId">The ID of the client</param>
        /// <returns>Connection information for the specified client</returns>
        ConnectionInfo GetConnectionInfo(int clientId);

        /// <summary>
        /// Gets the current connection state for a specific client
        /// </summary>
        /// <param name="clientId">The ID of the client</param>
        /// <returns>The current connection state</returns>
        ConnectionState GetConnectionState(int clientId);
        
        /// <summary>
        /// Sets the server information for this instance
        /// </summary>
        /// <param name="serverInfo">The server information to set</param>
        void SetServerInfo(ServerInfo serverInfo);

        /// <summary>
        /// Gets the current server information
        /// </summary>
        /// <returns>The current server information</returns>
        ServerInfo GetServerInfo();

        /// <summary>
        /// Updates the custom data in the server information
        /// </summary>
        /// <param name="customData">The custom data to update</param>
        void UpdateServerInfo(Dictionary<string, string> customData);
        
        /// <summary>
        /// Sets a bandwidth limit for the connection
        /// </summary>
        /// <param name="bytesPerSecond">The maximum number of bytes per second</param>
        void SetBandwidthLimit(int bytesPerSecond);
        
        /// <summary>
        /// Starts the server discovery process
        /// </summary>
        /// <param name="discoveryPort">The port to use for discovery, or -1 to use the default</param>
        void StartServerDiscovery(int discoveryPort = -1);

        /// <summary>
        /// Stops the server discovery process
        /// </summary>
        void StopServerDiscovery();

        /// <summary>
        /// Stops broadcasting server information
        /// </summary>
        void StopServerBroadcast();

        /// <summary>
        /// Broadcasts the current server information
        /// </summary>
        void BroadcastServerInfo();

        /// <summary>
        /// Triggers the OnClientConnected event
        /// </summary>
        /// <param name="id">The ID of the connected client</param>
        static void TriggerOnClientConnected(int id)
        {
            OnClientConnected?.Invoke(id);
        }

        /// <summary>
        /// Triggers the OnClientDisconnected event
        /// </summary>
        /// <param name="id">The ID of the disconnected client</param>
        static void TriggerOnClientDisconnected(int id)
        {
            OnClientDisconnected?.Invoke(id);
        }

        /// <summary>
        /// Triggers the OnDataReceived event
        /// </summary>
        /// <param name="id">The ID of the client that sent the data</param>
        static void TriggerOnDataReceived(int id)
        {
            OnDataReceived?.Invoke(id);
        }

        /// <summary>
        /// Triggers the OnLanServerDiscovered event
        /// </summary>
        /// <param name="serverInfo">Information about the discovered server</param>
        static void TriggerOnLanServersUpdate(ServerInfo serverInfo)
        {
            OnLanServerUpdate?.Invoke(serverInfo);
        }

        /// <summary>
        /// Triggers the OnConnectionStateChanged event
        /// </summary>
        /// <param name="connectionInfo">The updated connection information</param>
        static void TriggerOnConnectionStateChanged(ConnectionInfo connectionInfo)
        {
            OnConnectionStateChanged?.Invoke(connectionInfo);
        }
    }
}