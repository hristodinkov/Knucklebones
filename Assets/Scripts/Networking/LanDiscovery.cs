using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class LanDiscovery : MonoBehaviour
{
    public void DiscoverServers(System.Action<string, int> onServerFound, Action onTimeout = null)
    {
        UdpClient udp = new UdpClient();
        udp.EnableBroadcast = true;

        IPEndPoint broadcast = new IPEndPoint(IPAddress.Broadcast, 50007);
        byte[] msg = Encoding.UTF8.GetBytes("DISCOVER_KNUCKLEBONES");
        udp.Send(msg, msg.Length, broadcast);

        float timeout = Time.time + 3f;

        udp.BeginReceive((ar) =>
        {
            try
            {
                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udp.EndReceive(ar, ref remote);
                string text = Encoding.UTF8.GetString(data);

                if (text.StartsWith("KNUCKLEBONES_SERVER"))
                {
                    string[] parts = text.Split(':');
                    string ip = parts[1];
                    int port = int.Parse(parts[2]);

                    onServerFound?.Invoke(ip, port);
                }
            }
            catch
            {
                onTimeout?.Invoke();
            }
        }, null);
        StartCoroutine(TimeoutRoutine(() => onTimeout?.Invoke(), timeout));
    }

    private System.Collections.IEnumerator TimeoutRoutine(Action onTimeout, float timeout)
    {
        while (Time.time < timeout)
            yield return null;

        onTimeout?.Invoke();
    } 
}
