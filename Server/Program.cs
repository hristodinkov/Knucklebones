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
    static float pingInterval = 5000; // ms
    static float lastPingTime = 0;

    static Dictionary<TcpNetworkConnection, GameRoom> clientsToRoomMap = new Dictionary<TcpNetworkConnection, GameRoom>();
    static void Main() {
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
            HandleMessages(clients);

            lastPingTime += 10;
            if (lastPingTime > pingInterval)
            {
                lastPingTime = 0;
                foreach (var users in clients)
                {
                    if (users.Status == ConnectionStatus.Connected)
                    {
                        //Console.WriteLine($"Sending ping to client {users.Remote}");
                        OSCMessageOut ping = new OSCMessageOut("/Ping");
                        users.Send(ping.GetBytes());
                    }
                }
            }
            
            foreach (var room in rooms)
            {
                room.Tick();
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
                    if (string.IsNullOrEmpty(token))
                    {
                        Console.WriteLine("Invalid token received. Token ignored.");
                        continue;
                    }
                    HandlePlayerToken(client, token);
                    continue;
                }

                GameRoom room = FindRoomOfClient(client);
                if (room == null)
                {
                    continue;
                }
                int playerIndex = room.GetPlayerIndex(client);

                if (playerIndex < 0|| playerIndex > 1)
                {
                    Console.WriteLine("Client in room map but not found in room, ignoring.");
                    continue;
                }

                switch (addr)
                {
                    case "/ChooseDice":
                        int diceIndex = msg.ReadInt();
                        if (diceIndex < 0 || diceIndex > 1)
                        {
                            GameRoom cheatRoom = FindRoomOfClient(client);
                            if(cheatRoom == null)
                            {
                                Console.WriteLine($"Invalid dice index {diceIndex} from player {playerIndex}");
                            }
                            else
                            {
                                Console.WriteLine($"Invalid dice index {diceIndex} from player {playerIndex} in room {cheatRoom.GetHashCode()}");
                            }
                            break;
                        }
                        room.HandleChooseDice(playerIndex, diceIndex);
                        break;

                    case "/ChooseColumn":
                        int col = msg.ReadInt();
                        if (col < 0 || col > 2)
                        {
                            GameRoom cheatRoom = FindRoomOfClient(client);
                            if (cheatRoom == null)
                            {
                                Console.WriteLine($"Invalid column {col} from player {playerIndex}");
                            }
                            else
                            {
                                Console.WriteLine($"Invalid column {col} from player {playerIndex} in room {cheatRoom.GetHashCode()}");
                            }
                        }
                        room.HandleChooseColumn(playerIndex, col);
                        break;

                    case "/RequestRematch":
                        room.HandleRematch(playerIndex);
                        break;

                    case "/LeaveRoom":
                        room.HandleLeave(client);
                        clientsToRoomMap.Remove(client);
                        break;

                    default:
                        Console.WriteLine($"Unknown message: {addr}");
                        break;
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
                    Console.WriteLine($"Found room of client, calling HandleDisconnect");
                    room.HandleDisconnect(client);
                    clientsToRoomMap.Remove(client);
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
            //Console.WriteLine($"Room {i}: IsEmpty={rooms[i].IsEmpty()}, p1Disc={rooms[i].p1Disconnected}, p2Disc={rooms[i].p2Disconnected}");
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

