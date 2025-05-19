using UnityEngine;
using NetPackage.Network;
using System.Collections.Generic;
using NetPackage.Transport;
using UnityEditor;

namespace NetPackage.Network
{
    public class NetworkDebugWindow : MonoBehaviour
    {
        private bool showWindow = false;
        private Vector2 scrollPosition;
        private bool showDetailedInfo = false;
        private Dictionary<int, bool> clientFoldouts = new Dictionary<int, bool>();
        private Rect windowRect = new Rect(20, 20, 400, 600);
        private KeyCode toggleKey = KeyCode.F3;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showWindow = !showWindow;
            }
        }

        private void OnGUI()
        {
            if (!showWindow || !NetManager.DebugLog) return;

            windowRect = GUILayout.Window(0, windowRect, DrawWindow, "Network Debug");
        }

        private void DrawWindow(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            // Connection Status
            GUILayout.Label("Connection Status", EditorStyles.boldLabel);
            GUILayout.Label($"Is Host: {NetManager.IsHost}");
            GUILayout.Label($"Running: {NetManager.GetConnectionState()?.ToString() ?? "Not Connected"}");

            GUILayout.Space(10);

            // Server Information
            GUILayout.Label("Server Information", EditorStyles.boldLabel);
            var serverInfo = NetManager.GetServerInfo();
            if (serverInfo != null)
            {
                GUILayout.Label($"Server Name: {serverInfo.ServerName}");
                GUILayout.Label($"Address: {serverInfo.Address}");
                GUILayout.Label($"Port: {serverInfo.Port}");
                GUILayout.Label($"Current Players: {serverInfo.CurrentPlayers}");
                GUILayout.Label($"Max Players: {serverInfo.MaxPlayers}");
                GUILayout.Label($"Game Mode: {serverInfo.GameMode}");
            }
            else
            {
                GUILayout.Label("No server information available");
            }

            GUILayout.Space(10);

            // Client Information
            if (NetManager.IsHost)
            {
                GUILayout.Label("Connected Clients", EditorStyles.boldLabel);
                var clients = NetManager.GetClients();
                if (clients != null && clients.Count > 0)
                {
                    foreach (var client in clients)
                    {
                        GUILayout.Label($"Client {client.Id}: State: {client.State}");
                    }

                    GUILayout.Space(10);
                    showDetailedInfo = GUILayout.Toggle(showDetailedInfo, "Show Detailed Information");
                    if (showDetailedInfo)
                    {
                        foreach (var client in clients)
                        {
                            if (!clientFoldouts.ContainsKey(client.Id))
                            {
                                clientFoldouts[client.Id] = false;
                            }

                            clientFoldouts[client.Id] = GUILayout.Toggle(clientFoldouts[client.Id], $"Client {client.Id} Details");
                            if (clientFoldouts[client.Id])
                            {
                                var connectionInfo = NetManager.GetConnectionInfo(client.Id);
                                if (connectionInfo != null)
                                {
                                    GUILayout.Label($"Connection ID: {connectionInfo.Id}");
                                    GUILayout.Label($"State: {connectionInfo.State}");
                                    GUILayout.Label($"Bytes Received: {connectionInfo.BytesReceived}");
                                    GUILayout.Label($"Bytes Sent: {connectionInfo.BytesSent}");
                                    GUILayout.Label($"Last Ping: {connectionInfo.Ping}ms");
                                    GUILayout.Label($"Packet Loss: {connectionInfo.PacketLoss}");
                                    GUILayout.Label($"Connected Since: {connectionInfo.ConnectedSince:HH:mm:ss}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    GUILayout.Label("No clients connected");
                }
            }
            else
            {
                GUILayout.Label("Client Information", EditorStyles.boldLabel);
                var connectionInfo = NetManager.GetConnectionInfo();
                if (connectionInfo != null)
                {
                    GUILayout.Label($"Connection ID: {connectionInfo.Id}");
                    GUILayout.Label($"State: {connectionInfo.State}");
                    GUILayout.Label($"Bytes Received: {connectionInfo.BytesReceived}");
                    GUILayout.Label($"Bytes Sent: {connectionInfo.BytesSent}");
                    GUILayout.Label($"Last Ping: {connectionInfo.Ping}ms");
                    GUILayout.Label($"Packet Loss: {connectionInfo.PacketLoss}");
                    GUILayout.Label($"Connected Since: {connectionInfo.ConnectedSince:HH:mm:ss}");
                }
                else
                {
                    GUILayout.Label("Not connected");
                }
            }

            GUILayout.EndScrollView();

            // Make the window draggable
            GUI.DragWindow();
        }
    }
} 