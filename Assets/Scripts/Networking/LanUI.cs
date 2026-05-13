using UnityEngine;
using System.Collections.Generic;

public class LanUI : MonoBehaviour
{
    public Client client;
    public LanDiscovery discovery;

    bool searching = false;
    string status = "";
    List<(string ip, int port)> servers = new List<(string, int)>();

    void OnGUI()
    {
        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.fontSize = 20;
        title.fontStyle = FontStyle.Bold;

        GUILayout.BeginArea(new Rect(20, 20, 400, 600));

        GUILayout.Label("LAN Matchmaking", title);
        GUILayout.Space(20);
        if (!searching)
        {
            if (GUILayout.Button("Find Game", GUILayout.Height(40)))
            {
                StartSearch();
            }
        }

        if (status != "")
        {
            GUILayout.Label(status);
        }

       
        foreach (var s in servers)
        {
            if (GUILayout.Button($"{s.ip}:{s.port}", GUILayout.Height(30)))
            {
                status = $"Connecting to {s.ip}:{s.port}...";
                //client.ConnectTo(s.ip, s.port);
            }
        }

        GUILayout.EndArea();
    }

    void StartSearch()
    {
        searching = true;
        status = "Searching for servers...";
        servers.Clear();

        discovery.DiscoverServers(
            (ip, port) =>
            {
                servers.Add((ip, port));
                status = "Select a server:";
            },
            () =>
            {
                if (servers.Count == 0)
                    status = "No servers found.";
            }
        );
    }
}
