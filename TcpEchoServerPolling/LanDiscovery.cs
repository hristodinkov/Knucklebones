using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class LanDiscovery
{
    public static void Start(int tcpPort)
    {
        new Thread(() =>
        {
            UdpClient udp = new UdpClient(new IPEndPoint(IPAddress.Any, 50007)); // UDP discovery port
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine("UDP discovery server running on port 50007");

            while (true)
            {
                byte[] data = udp.Receive(ref remote);
                string msg = Encoding.UTF8.GetString(data);

                if (msg == "DISCOVER_KNUCKLEBONES")
                {
                    Console.WriteLine("Received UDP: " + msg);
                    string response = $"KNUCKLEBONES_SERVER:{GetLocalIPAddress()}:{tcpPort}";
                    byte[] respBytes = Encoding.UTF8.GetBytes(response);
                    udp.Send(respBytes, respBytes.Length, remote);
                }
            }
        })
        { IsBackground = true }.Start(); 
    }

    private static string GetLocalIPAddress()
    {
        foreach (var ni in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ni.AddressFamily == AddressFamily.InterNetwork)
                return ni.ToString();
        }
        return "127.0.0.1";
    }
}

