using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public static class LanDiscovery
{
    private static bool running = false;

    public static void Start(int tcpPort)
    {
        if (running) return;
        running = true;

        new Thread(() =>
        {
            try
            {
                UdpClient udp = new UdpClient(new IPEndPoint(IPAddress.Any, 50007));
                udp.Client.EnableBroadcast = true;
                udp.Client.MulticastLoopback = false;

                Console.WriteLine("LAN Discovery server running on UDP 50007");

                while (running)
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udp.Receive(ref remote);

                    string msg = Encoding.UTF8.GetString(data);

                    if (msg == "DISCOVER_KNUCKLEBONES")
                    {
                        string ip = GetLocalLANIP();
                        string response = $"KNUCKLEBONES_SERVER:{ip}:{tcpPort}";
                        byte[] respBytes = Encoding.UTF8.GetBytes(response);

                        udp.Send(respBytes, respBytes.Length, remote);
                        Console.WriteLine($"Discovery request from {remote.Address}, responded with {ip}:{tcpPort}");
                    }
                }

                udp.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("LAN Discovery error: " + ex.Message);
            }

        })
        { IsBackground = true }.Start();
    }

    public static void Stop()
    {
        running = false;
    }

    private static string GetLocalLANIP()
    {
        foreach (var ni in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ni.AddressFamily == AddressFamily.InterNetwork)
            {
                string ip = ni.ToString();

                // Only return real LAN IPs
                if (ip.StartsWith("192.168.") ||
                    ip.StartsWith("10.") ||
                    ip.StartsWith("172."))
                {
                    return ip;
                }
            }
        }

        // Fallback
        return "127.0.0.1";
    }
}
