using System.Net; // For IPAddress
using System.Net.Sockets; // For TcpListener, TcpClient
using NetworkConnections;
using TcpEchoServerPolling; // For TcpNetworkConnection
using OSCTools;
using System.Collections.Generic;
using System;
using System.Threading;

class TcpServer {

	static int port = 50001;
	static List<TcpNetworkConnection> clients = new List<TcpNetworkConnection>();
	static List<GameRoom> rooms = new List<GameRoom>();
    static float pingInterval = 300; // ms
    static float lastPingTime = 0;

    static Dictionary<TcpNetworkConnection, GameRoom> clientsToRoomMap = new Dictionary<TcpNetworkConnection, GameRoom>();
    static Dictionary<TcpNetworkConnection, int> playerID = new Dictionary<TcpNetworkConnection, int>();
    static void Main() {
        //LanDiscovery.Start(port);
        StartServer(port);
	}

	static void StartServer(int port) {
		// Start listening for TCP connection requests, on the given port:
		TcpListener listener = new TcpListener(IPAddress.Any, port);
		listener.Start();
		Console.WriteLine($"Starting TCP server on port {port}"); 
        Console.WriteLine("Press Q to stop the server");

		while (true) {
			// Note: there is no error handling in this server! Is it needed? If so, where?

			AcceptNewClients(listener, clients);
            //foreach (var client in clients)
            //{
            //    client.Available();
            //}

            HandleMessages(clients);
            foreach (var client in clients)
            {

                if (client.Status == ConnectionStatus.Connected)
                {
                    try
                    {
                        if (lastPingTime > pingInterval)
                        {
                            OSCMessageOut ping = new OSCMessageOut("/Ping");
                            client.Send(ping.GetBytes());
                            lastPingTime = 0;

                        }
                        lastPingTime += 10;
                    }
                    catch
                    {
                        // Send() already calls Close() internally
                    }
                }
            }
            foreach (var room in rooms)
            {
                room.Tick(10);
            }
            CheckAndCleanupClients(clients);
			CleanupRooms();
			if (QuitPressed()) {
				Console.WriteLine("Stopping server");
				break;
			}
			// It's good to give the CPU a break - 10ms is enough, and still gives fast response times:
			Thread.Sleep(10);
		}
		// When stopping the server, properly clean up all resources:
		foreach (TcpNetworkConnection client in clients) {
			client.Close();
		}
		listener.Stop();
		Console.WriteLine("Server stopped");
	}

    

    static void AcceptNewClients(TcpListener listener, List<TcpNetworkConnection> clients)
    {
        // Pending will be true if there is an incoming connection request:
        if (!listener.Pending())
        {
            return;
        }
        // ..if so, accept it and store the new TcpClient:
        // (Note that the AcceptTcpClient call is not blocking now, since we know there's a pending request!)
        TcpClient newClient = listener.AcceptTcpClient();
        TcpNetworkConnection connection = new TcpNetworkConnection(newClient);
        clients.Add(connection);
        Console.WriteLine($"Client connected from remote end point {newClient.Client.RemoteEndPoint}");
        

    }

    static void AssignClientToRoom(TcpNetworkConnection connection,string token)
    {
        GameRoom targetRoom = null;
        foreach (var room in rooms)
        {
            if (!room.IsFull()&&!room.gameStarted)
            {
                targetRoom = room;
                break;
            }
        }
        if(targetRoom == null)
        {
            targetRoom =new GameRoom();
            rooms.Add(targetRoom);
            Console.WriteLine("New room created");
        }

        targetRoom.AddPlayer(connection,token);
        clientsToRoomMap[connection] = targetRoom;

    }
    static GameRoom FindRoomOfClient(TcpNetworkConnection connection)
    {
        if (clientsToRoomMap.TryGetValue(connection, out GameRoom room))
        {
            return room;
        }  
        return null;
    }
    static void HandleMessages(List<TcpNetworkConnection> clients) {
        foreach (TcpNetworkConnection client in clients)
        {
            
            // For each of the connected clients, we check whether there's an incoming message available:
            if (client.Status != ConnectionStatus.Connected)
            {
                continue;
            }
            // ..if so, we read exactly that many bytes into an array:
            //NetworkStream stream = client.;
            //int packetLength = client.Available();
            while (client.Available() > 0)
            {
                byte[] data = client.GetPacket();
                if (data == null)
                {
                    break;
                }
                OSCMessageIn msg = new OSCMessageIn(data);
                string addr = msg.header;

                if (addr == "/RequestPlayerID")
                { 
                    string token = msg.ReadString();
                    HandlePlayerToken(client, token);
                    continue;
                }

                GameRoom room = FindRoomOfClient(client);
                if (room == null)
                {
                    continue;
                }
                int playerIndex = room.GetPlayerIndex(client);

                if (addr == "/ChooseDice")
                {
                    int diceIndex = msg.ReadInt();
                    room.HandleChooseDice(playerIndex, diceIndex);
                }
                else if (addr == "/ChooseColumn")
                {
                    int col = msg.ReadInt();
                    room.HandleChooseColumn(playerIndex, col);
                }
                else if (addr == "/RequestRematch")
                {
                    room.HandleRematch(playerIndex);
                }
                else if(addr =="/LeaveRoom")
                {
                    GameRoom clientRoom = FindRoomOfClient(client);
                    if (clientRoom != null)
                    {
                        clientRoom.HandleLeave(client);
                        clientsToRoomMap.Remove(client);
                    }
                }

            }

        }
    }

    static void HandlePlayerToken(TcpNetworkConnection connection, string token)
    {
        foreach (var room in rooms)
        {
            if (room.p1Token == token && (room.p1Disconnected || room.player1 == null))
            {
                if (room.player1 != null) 
                {
                    clientsToRoomMap.Remove(room.player1);
                }
                room.ReconnectPlayer(connection, 0);
                clientsToRoomMap[connection] = room;
                return;
            }
            if (room.p2Token == token && (room.p2Disconnected || room.player2 == null))
            {
                if (room.player2 != null)
                {
                    clientsToRoomMap.Remove(room.player2);
                }
                room.ReconnectPlayer(connection, 1);
                clientsToRoomMap[connection] = room; 
                return;
            }
        }
        AssignClientToRoom(connection, token);
    }


    static void CheckAndCleanupClients(List<TcpNetworkConnection> clients) {
		for (int i = clients.Count - 1; i >= 0; i--) {
            // If any of our current clients are disconnected, 
            // we close the TcpClient to clean up resources, and remove it from our list:
            // (Note that this type of for loop is needed since we're modifying the collection inside the loop!) 
            if (clients[i].Status == ConnectionStatus.Disconnected) {
                TcpNetworkConnection client = clients[i];
                Console.WriteLine($"Detected disconnected client: {client.Remote}");

                if (clientsToRoomMap.TryGetValue(client, out GameRoom room))
                {
                    Console.WriteLine($"Found room for client, calling HandleDisconnect");
                    room.HandleDisconnect(client);
                    clientsToRoomMap.Remove(client);
                }
                else
                {
                    Console.WriteLine($"NO ROOM FOUND for disconnected client!");
                }
                client.Close();
                clients.RemoveAt(i);
            }
		}
	}
    static void CleanupRooms()
    {
        for (int i = rooms.Count - 1; i >= 0; i--)
        {
            if (rooms[i].IsEmpty())
            {
                Console.WriteLine("Removing empty room");
                rooms.RemoveAt(i);
            }
        }
    }
    static bool QuitPressed() {
		if (Console.KeyAvailable) {
			char input = Console.ReadKey(true).KeyChar;
			if (input == 'q') {
				return true;
			}
		}
		return false;
	}
}

