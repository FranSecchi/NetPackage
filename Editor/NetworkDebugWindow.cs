using UnityEngine;
using UnityEditor;
using NetPackage.Network;
using System.Collections.Generic;
using System.Linq;
using NetPackage.Transport;
using NetPackage.Synchronization;

namespace NetPackage.Editor
{
    public class NetworkDebugWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private Vector2 messageScrollPosition;
        private bool autoRefresh = true;
        private float refreshInterval = 1f;
        private double lastRefreshTime;
        private bool showDetailedInfo = false;
        private bool showNetObjects = false;
        private bool showMessages = false;
        private Dictionary<int, bool> clientFoldouts = new Dictionary<int, bool>();
        private Dictionary<int, bool> netObjectFoldouts = new Dictionary<int, bool>();
        private bool[] messageTypeFilters;
        private string messageSearchText = "";

        // Last known state fields
        private static ServerInfo lastKnownServerInfo;
        private static List<ConnectionInfo> lastKnownClients;
        private static List<NetObject> lastKnownNetObjects;
        private static bool lastKnownIsHost;
        private static ConnectionState? lastKnownConnectionState;

        [MenuItem("Window/NetPackage/Network Debug")]
        public static void ShowWindow()
        {
            GetWindow<NetworkDebugWindow>("Network Debug");
        }

        private void OnEnable()
        {
            messageTypeFilters = new bool[System.Enum.GetValues(typeof(DebugQueue.MessageType)).Length];
            for (int i = 0; i < messageTypeFilters.Length; i++)
            {
                messageTypeFilters[i] = true;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            // Handle auto-refresh
            if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > refreshInterval)
            {
                Repaint();
                lastRefreshTime = EditorApplication.timeSinceStartup;
            }

            // Update last known state
            if (Application.isPlaying)
            {
                UpdateLastKnownState();
            }
        }

        private void UpdateLastKnownState()
        {
            try
            {
                var newServerInfo = NetManager.GetServerInfo();
                if (newServerInfo != null)
                {
                    lastKnownServerInfo = newServerInfo;
                }
            }
            catch (System.Exception) { }

            try
            {
                lastKnownIsHost = NetManager.IsHost;
            }
            catch (System.Exception) { }

            try
            {
                var newState = NetManager.GetConnectionState();
                if (newState.HasValue)
                {
                    lastKnownConnectionState = newState;
                }
            }
            catch (System.Exception) { }

            try
            {
                if (NetManager.IsHost)
                {
                    var newClients = NetManager.GetClients();
                    if (newClients != null)
                    {
                        lastKnownClients = newClients;
                    }
                }
            }
            catch (System.Exception) { }

            try
            {
                var newNetObjects = NetScene.GetAllNetObjects();
                if (newNetObjects != null && newNetObjects.Count > 0)
                {
                    // Create a new list to store the objects
                    lastKnownNetObjects = new List<NetObject>();
                    foreach (var netObj in newNetObjects)
                    {
                        if (netObj != null)
                        {
                            lastKnownNetObjects.Add(netObj);
                        }
                    }
                }
            }
            catch (System.Exception) { }
        }

        private void DrawMessageLog()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Message Log", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // Message filters
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(50));
            for (int i = 0; i < messageTypeFilters.Length; i++)
            {
                var type = (DebugQueue.MessageType)i;
                EditorGUILayout.BeginHorizontal(GUILayout.Width(100));
                messageTypeFilters[i] = EditorGUILayout.Toggle(messageTypeFilters[i], GUILayout.Width(20));
                EditorGUILayout.LabelField(type.ToString(), GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();

            // Search box
            messageSearchText = EditorGUILayout.TextField("Search", messageSearchText);

            // Clear button
            if (GUILayout.Button("Clear Messages"))
            {
                DebugQueue.ClearMessages();
            }

            messageScrollPosition = EditorGUILayout.BeginScrollView(messageScrollPosition, GUILayout.Height(200));

            var messages = DebugQueue.GetMessages();
            var filteredMessages = messages.Where(m => 
                messageTypeFilters[(int)m.Type] && 
                (string.IsNullOrEmpty(messageSearchText) || m.Message.ToLower().Contains(messageSearchText.ToLower()))
            ).ToList();

            foreach (var message in filteredMessages)
            {
                Color originalColor = GUI.color;
                switch (message.Type)
                {
                    case DebugQueue.MessageType.Error:
                        GUI.color = Color.red;
                        break;
                    case DebugQueue.MessageType.Warning:
                        GUI.color = Color.yellow;
                        break;
                    case DebugQueue.MessageType.Network:
                        GUI.color = Color.cyan;
                        break;
                    case DebugQueue.MessageType.RPC:
                        GUI.color = Color.green;
                        break;
                    case DebugQueue.MessageType.State:
                        GUI.color = Color.magenta;
                        break;
                }

                EditorGUILayout.LabelField($"[{message.Timestamp:F2}s] {message.Message}");
                GUI.color = originalColor;
            }

            EditorGUILayout.EndScrollView();
            EditorGUI.indentLevel--;
        }

        private void DrawConnectionStatus()
        {
            EditorGUILayout.LabelField("Connection Status", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            try
            {
                bool isHost = Application.isPlaying ? NetManager.IsHost : lastKnownIsHost;
                ConnectionState? state = Application.isPlaying ? NetManager.GetConnectionState() : lastKnownConnectionState;
                
                EditorGUILayout.LabelField("Is Host", isHost.ToString());
                EditorGUILayout.LabelField("Running", state?.ToString() ?? "Not Connected");
            }
            catch (System.Exception)
            {
                EditorGUILayout.LabelField("Status", "Unable to retrieve connection status");
            }
            EditorGUI.indentLevel--;
        }

        private void DrawServerInformation()
        {
            EditorGUILayout.LabelField("Server Information", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            try
            {
                var serverInfo = Application.isPlaying ? NetManager.GetServerInfo() : lastKnownServerInfo;
                if (serverInfo != null)
                {
                    EditorGUILayout.LabelField("Server Name", serverInfo.ServerName);
                    EditorGUILayout.LabelField("Address", serverInfo.Address);
                    EditorGUILayout.LabelField("Port", serverInfo.Port.ToString());
                    EditorGUILayout.LabelField("Current Players", serverInfo.CurrentPlayers.ToString());
                    EditorGUILayout.LabelField("Max Players", serverInfo.MaxPlayers.ToString());
                    EditorGUILayout.LabelField("Game Mode", serverInfo.GameMode);
                }
                else
                {
                    EditorGUILayout.LabelField("No server information available");
                }
            }
            catch (System.Exception)
            {
                EditorGUILayout.LabelField("Unable to retrieve server information");
            }
            EditorGUI.indentLevel--;
        }

        private void DrawClientInformation()
        {
            try
            {
                bool isHost = Application.isPlaying ? NetManager.IsHost : lastKnownIsHost;
                if (isHost)
                {
                    EditorGUILayout.LabelField("Connected Clients", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    var clients = Application.isPlaying ? NetManager.GetClients() : lastKnownClients;
                    if (clients != null && clients.Count > 0)
                    {
                        foreach (var client in clients)
                        {
                            EditorGUILayout.LabelField($"Client {client.Id}", $"State: {client.State}");
                        }

                        EditorGUILayout.Space();
                        showDetailedInfo = EditorGUILayout.Foldout(showDetailedInfo, "Detailed Connection Information", true);
                        if (showDetailedInfo)
                        {
                            EditorGUI.indentLevel++;
                            foreach (var client in clients)
                            {
                                if (!clientFoldouts.ContainsKey(client.Id))
                                {
                                    clientFoldouts[client.Id] = false;
                                }

                                clientFoldouts[client.Id] = EditorGUILayout.Foldout(clientFoldouts[client.Id], $"Client {client.Id} Details", true);
                                if (clientFoldouts[client.Id])
                                {
                                    EditorGUI.indentLevel++;
                                    ConnectionInfo connectionInfo = Application.isPlaying ? NetManager.GetConnectionInfo(client.Id) : client;
                                    if (connectionInfo != null)
                                    {
                                        EditorGUILayout.LabelField("Connection ID", connectionInfo.Id.ToString());
                                        EditorGUILayout.LabelField("State", connectionInfo.State.ToString());
                                        EditorGUILayout.LabelField("Bytes Received", connectionInfo.BytesReceived.ToString() ?? "Unknown");
                                        EditorGUILayout.LabelField("Bytes Sent", connectionInfo.BytesSent.ToString());
                                        EditorGUILayout.LabelField("Last Ping", $"{connectionInfo.Ping}ms");
                                        EditorGUILayout.LabelField("Packet Loss", connectionInfo.PacketLoss.ToString());
                                        EditorGUILayout.LabelField("Connected Since", connectionInfo.ConnectedSince.ToString("HH:mm:ss"));
                                    }
                                    EditorGUI.indentLevel--;
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No clients connected");
                    }
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.LabelField("Client Information", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    var connectionInfo = Application.isPlaying ? NetManager.GetConnectionInfo() : (lastKnownClients?.FirstOrDefault());
                    if (connectionInfo != null)
                    {
                        EditorGUILayout.LabelField("Connection ID", connectionInfo.Id.ToString());
                        EditorGUILayout.LabelField("State", connectionInfo.State.ToString());
                        EditorGUILayout.LabelField("Bytes Received", connectionInfo.BytesReceived.ToString() ?? "Unknown");
                        EditorGUILayout.LabelField("Bytes Sent", connectionInfo.BytesSent.ToString());
                        EditorGUILayout.LabelField("Last Ping", $"{connectionInfo.Ping}ms");
                        EditorGUILayout.LabelField("Packet Loss", connectionInfo.PacketLoss.ToString());
                        EditorGUILayout.LabelField("Connected Since", connectionInfo.ConnectedSince.ToString("HH:mm:ss"));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Not connected");
                    }
                    EditorGUI.indentLevel--;
                }
            }
            catch (System.Exception)
            {
                EditorGUILayout.LabelField("Unable to retrieve client information");
            }
        }

        private void DrawNetworkObjects()
        {
            EditorGUILayout.LabelField("Network Objects", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            try
            {
                showNetObjects = EditorGUILayout.Foldout(showNetObjects, "Network Objects List", true);
                if (showNetObjects)
                {
                    var netObjects = Application.isPlaying ? NetScene.GetAllNetObjects() : lastKnownNetObjects;
                    if (netObjects != null && netObjects.Count > 0)
                    {
                        foreach (var netObj in netObjects)
                        {
                            if (!netObjectFoldouts.ContainsKey(netObj.NetId))
                            {
                                netObjectFoldouts[netObj.NetId] = false;
                            }

                            netObjectFoldouts[netObj.NetId] = EditorGUILayout.Foldout(netObjectFoldouts[netObj.NetId], 
                                $"NetObject {netObj.NetId} - {netObj.SceneId}", true);
                            
                            if (netObjectFoldouts[netObj.NetId])
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.LabelField("Network ID: " + netObj.NetId.ToString());
                                EditorGUILayout.LabelField("Scene ID: " + (string.IsNullOrEmpty(netObj.SceneId) ? "(null)" : netObj.SceneId.ToString()));
                                EditorGUILayout.LabelField("Owner: " + netObj.OwnerId.ToString());
                                EditorGUILayout.LabelField("Is Scene Object: "+ !string.IsNullOrEmpty(netObj.SceneId));
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No network objects found");
                    }
                }
            }
            catch (System.Exception)
            {
                EditorGUILayout.LabelField("Unable to retrieve network objects");
            }
            EditorGUI.indentLevel--;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Showing last known state from previous play session.", MessageType.Info);
            }

            // Auto-refresh toggle (only in play mode)
            if (Application.isPlaying)
            {
                autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
                if (autoRefresh)
                {
                    refreshInterval = EditorGUILayout.Slider("Refresh Interval", refreshInterval, 0.1f, 5f);
                }
                else if (GUILayout.Button("Refresh"))
                {
                    Repaint();
                }
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawConnectionStatus();
            EditorGUILayout.Space();
            DrawServerInformation();
            EditorGUILayout.Space();
            DrawClientInformation();
            EditorGUILayout.Space();
            DrawNetworkObjects();

            // Add Message Log section
            showMessages = EditorGUILayout.Foldout(showMessages, "Message Log", true);
            if (showMessages)
            {
                DrawMessageLog();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
} 