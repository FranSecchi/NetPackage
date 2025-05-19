using UnityEngine;
using UnityEditor;
using NetPackage.Network;
using System.Collections.Generic;

namespace NetPackage.Editor
{
    public class NetworkDebugWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool autoRefresh = true;
        private float refreshInterval = 1f;
        private double lastRefreshTime;
        private bool showDetailedInfo = false;
        private Dictionary<int, bool> clientFoldouts = new Dictionary<int, bool>();

        [MenuItem("Window/NetPackage/Network Debug")]
        public static void ShowWindow()
        {
            GetWindow<NetworkDebugWindow>("Network Debug");
        }

        private void OnGUI()
        {
            if (!NetManager.DebugLog)
            {
                EditorGUILayout.HelpBox("Debug logging is disabled in NetManager. Enable it to see network information.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical();

            // Auto-refresh toggle
            autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
            if (autoRefresh)
            {
                refreshInterval = EditorGUILayout.Slider("Refresh Interval", refreshInterval, 0.1f, 5f);
                if (EditorApplication.timeSinceStartup - lastRefreshTime > refreshInterval)
                {
                    Repaint();
                    lastRefreshTime = EditorApplication.timeSinceStartup;
                }
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
                                var connectionInfo = NetManager.GetConnectionInfo(client.Id);
                                if (connectionInfo != null)
                                {
                                    EditorGUILayout.LabelField("Connection ID", connectionInfo.Id.ToString());
                                    EditorGUILayout.LabelField("State", connectionInfo.State.ToString());
                                    EditorGUILayout.LabelField("Address", connectionInfo.Address ?? "Unknown");
                                    EditorGUILayout.LabelField("Port", connectionInfo.Port.ToString());
                                    EditorGUILayout.LabelField("Last Ping", $"{connectionInfo.LastPing}ms");
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
                    EditorGUILayout.LabelField("Address", connectionInfo.Address ?? "Unknown");
                    EditorGUILayout.LabelField("Port", connectionInfo.Port.ToString());
                    EditorGUILayout.LabelField("Last Ping", $"{connectionInfo.LastPing}ms");
                    EditorGUILayout.LabelField("Connected Since", connectionInfo.ConnectedSince.ToString("HH:mm:ss"));
                }
                else
                {
                    EditorGUILayout.LabelField("Not connected");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
} 