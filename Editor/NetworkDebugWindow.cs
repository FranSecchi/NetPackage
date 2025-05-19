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
        private bool autoRefresh = true;
        private float refreshInterval = 1f;
        private double lastRefreshTime;
        private bool showDetailedInfo = false;
        private bool showNetObjects = false;
        private Dictionary<int, bool> clientFoldouts = new Dictionary<int, bool>();
        private Dictionary<int, bool> netObjectFoldouts = new Dictionary<int, bool>();
        private bool wasDebugEnabled = false;

        [MenuItem("Window/NetPackage/Network Debug")]
        public static void ShowWindow()
        {
            GetWindow<NetworkDebugWindow>("Network Debug");
        }

        private void Update()
        {
            // Check if debug state changed
            if (wasDebugEnabled != NetManager.DebugLog)
            {
                wasDebugEnabled = NetManager.DebugLog;
                if (wasDebugEnabled)
                {
                    // If debug was enabled, show the window
                    ShowWindow();
                }
            }

            // Handle auto-refresh
            if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > refreshInterval)
            {
                Repaint();
                lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Network debug information is only available in Play Mode.", MessageType.Info);
                return;
            }

            if (!NetManager.DebugLog)
            {
                EditorGUILayout.HelpBox("Debug logging is disabled in NetManager. Enable it to see network information.", MessageType.Warning);
                if (GUILayout.Button("Enable Debug Logging"))
                {
                    if(Application.isPlaying) NetManager.DebugLog = true;
                }
                return;
            }

            EditorGUILayout.BeginVertical();

            // Auto-refresh toggle
            autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
            if (autoRefresh)
            {
                refreshInterval = EditorGUILayout.Slider("Refresh Interval", refreshInterval, 0.1f, 5f);
            }
            else if (GUILayout.Button("Refresh"))
            {
                Repaint();
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Connection Status
            EditorGUILayout.LabelField("Connection Status", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Is Host", NetManager.IsHost.ToString());
            EditorGUILayout.LabelField("Running", NetManager.GetConnectionState()?.ToString() ?? "Not Connected");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            // Server Information
            EditorGUILayout.LabelField("Server Information", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            var serverInfo = NetManager.GetServerInfo();
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
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            // Client Information
            if (NetManager.IsHost)
            {
                EditorGUILayout.LabelField("Connected Clients", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                var clients = NetManager.GetClients();
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
                                ConnectionInfo connectionInfo = NetManager.GetConnectionInfo(client.Id);
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
                var connectionInfo = NetManager.GetConnectionInfo();
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

            EditorGUILayout.Space();

            // Network Objects Information
            EditorGUILayout.LabelField("Network Objects", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            showNetObjects = EditorGUILayout.Foldout(showNetObjects, "Network Objects List", true);
            if (showNetObjects)
            {
                var netObjects = NetScene.Instance.GetAllNetObjects();
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
                            EditorGUILayout.LabelField("Is Scene Object: "+ string.IsNullOrEmpty(netObj.SceneId));
                            EditorGUI.indentLevel--;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No network objects found");
                }
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
} 