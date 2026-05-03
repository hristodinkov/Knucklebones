using NetworkConnections;
using OSCTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// The Server is the class that manages network connections with all clients, and 
/// communicates with the game code (Model classes).
/// </summary>
public class Server : MonoBehaviour
{
    // ----- Debugging
    [SerializeField] private bool showDebugMessages = true;

	// ----- General server code:
	TcpListener listener;
	List<TcpNetworkConnection> connections;
	OSCDispatcher dispatcher;

	/// ------ TicTacToe Server code:
	//TicTacToeBoard board;
	private Model p1Model;
	private Model p2Model;
	private bool turnorder;
	private int dice1, dice2;
	private Dictionary<TcpNetworkConnection, int> playerIDs = new Dictionary<TcpNetworkConnection, int>();
    private int selectedDiceP1 =-1;
    private int selectedDiceP2 =-1;

    void Start()
    {
		p1Model = new Model(1); 
		p2Model = new Model(2);
		turnorder = true;
        // This server starts with a listener:
        int port = 50006;
		Log("Starting server at " + port );
		listener = new TcpListener(IPAddress.Any, port);
		listener.Start();

		connections = new List<TcpNetworkConnection>();

		// Initialize the dispatcher and callbacks for incoming OSC messages:
		dispatcher = new OSCDispatcher();
		dispatcher.ShowIncomingMessages = true;
		Initialize();
    }

    void Update()
    {
        if (listener == null)
            return;
        AcceptNewConnections();
		UpdateConnections();
		CleanupConnections();
    }

	void AcceptNewConnections() {
		if (listener.Pending()) {
			TcpClient client = listener.AcceptTcpClient();
			TcpNetworkConnection connection = new TcpNetworkConnection(client);
			connections.Add(connection);
			Log("Server: Adding new connection from " + connection.Remote);
			ClientJoined(connection);
		}
	}
	void ClientJoined(TcpNetworkConnection newClient) {
		if (playerIDs.Count <= 2)
		{
			int assignedID = playerIDs.Count;
			playerIDs[newClient] = assignedID;
			SendPrivateInformationCommand(assignedID, newClient);

            if (playerIDs.Count == 2)
            {
                print("Players are connected. Starting game.");
                turnorder = true;
                BroadcastTurnChange(0);
                RollDice();
            }
        }
		else
		{
			Log("Sorry - already have 2 players");
			// Note: this client is still allowed to join as spectator, but not as player!
			// TODO: Send a message to this client
		}
	}

	void UpdateConnections() {
		foreach (TcpNetworkConnection conn in connections) {
			// The connection will call HandlePacket when a packet is available:
			while (conn.Available()>0) {
				HandlePacket(conn.GetPacket(), conn.Remote);
			}
		}
	}

	void HandlePacket(byte[] packet, IPEndPoint remote) {
		OSCMessageIn mess = new OSCMessageIn(packet);
		Log("Message arrives on server: " + mess);

		dispatcher.HandlePacket(packet, remote);
	}

    int GetPlayerID(IPEndPoint remote)
    {
        foreach (var playerData in playerIDs)
        {
            TcpNetworkConnection connection = playerData.Key;
            int playerID = playerData.Value;

            // Compare endpoints
            if (connection.Remote.Equals(remote))
            {
                return playerID;
            }
        }

        Debug.LogError("Server: Could not find playerID for remote " + remote);
        return -1; // invalid
    }


    void CleanupConnections() {
		// TODO
	}

	void Initialize() {
        dispatcher.AddListener("/ChooseDice", ChooseDiceRpc, OSCUtil.INT);
        dispatcher.AddListener("/ChooseColumn", ChooseColumnRpc, OSCUtil.INT);
        dispatcher.AddListener("/RequestRematch", RematchRpc);

    }
    //Server logic
    void RollDice()
    {
        dice1 = UnityEngine.Random.Range(1, 7);
        dice2 = UnityEngine.Random.Range(1, 7);

        BroadcastDiceRolled(dice1, dice2);
    }
    void HandlePlaceDice(int player, int col)
    {
        int expectedPlayer;
        if (turnorder)
        {
            expectedPlayer = 0;
        }
        else
        {
            expectedPlayer = 1;
        }
        if (player != expectedPlayer)
        {
            Log($"Server: Player {player} tried to move out of turn.");
            return;
        }
        int selected;
        Model add;
        Model remove;

        if (player == 0)
        {
            selected = selectedDiceP1;
            add = p1Model;
            remove = p2Model;
        }
        else
        {
            selected = selectedDiceP2;
            add = p2Model;
            remove = p1Model;
        }
        if(selected==-1)
        {
            Log($"Server: Player {player} has not selected a dice.");
            return;
        }

        bool success = add.TryAddNewDice(selected, col);
        if (!success) return;

        remove.TryRemoveNumber(col, selected);

        if (player == 0)
        {
            selectedDiceP1 = -1;
        }
        else 
        { 
            selectedDiceP2 = -1; 
        }


        BroadcastGridUpdate(player, col);
        int opponent;
        if(player == 1)
        {
            opponent = 0;
        }
        else
        {
            opponent = 1;
        }
        BroadcastGridUpdate(opponent, col);
        BroadcastScoreUpdate();

        if (CheckGameOver(add.grid))
        {
            int scoreP1 = p1Model.CalculateGridScore();
            int scoreP2 = p2Model.CalculateGridScore();
            int winner;
            if (scoreP1 > scoreP2)
            {
                winner = 0;
            }
            else if (scoreP2 > scoreP1)
            {
                winner = 1;
            }
            else
            {
                winner = -1;
            }
            GameOverRpc(winner);
            return;
        }
        else
        {
            turnorder = !turnorder;
            BroadcastTurnChange(turnorder ? 0 : 1);

            RollDice();
        } 
    }

    private bool CheckGameOver(int[,] grid)
    {
        foreach (int cell in grid)
        {
            if (cell == 0) return false;
        }
        return true;
    }

    void ResetGame()
    {
        p1Model = new Model(1);
        p2Model = new Model(2);

        selectedDiceP1 = -1;
        selectedDiceP2 = -1;

        turnorder = true;
    }


    // ----- Handle incoming RPCs (called by dispatcher):

    void ChooseDiceRpc(OSCMessageIn msg, IPEndPoint remote)
    {
        //Log($"Server: message arrives on server: {msg.ToString()} - diceIndex");
        int diceIndex = msg.ReadInt();
        int player = GetPlayerID(remote);

        int chosenValue;

        if (diceIndex == 0)
        {
            chosenValue = dice1;
        }
        else
        {
            chosenValue = dice2;
        }

        if (player == 0)
        {
            selectedDiceP1 = chosenValue;
        }
        else
        {
            selectedDiceP2 = chosenValue;
        }

        Log($"Server: Player {player} selected dice index {diceIndex} with value {chosenValue}");
    }
    void ChooseColumnRpc(OSCMessageIn msg, IPEndPoint remote)
    {
        Log($"Server: message arrives on server: {msg.ToString()} - col");
        int col = msg.ReadInt();
        int player = GetPlayerID(remote);

        HandlePlaceDice(player, col);
    }

    void RematchRpc(OSCMessageIn msg, IPEndPoint remote)
    {
        int player = GetPlayerID(remote);
        Log($"Player {player} requested a rematch.");

        // For now: restart immediately when ANY player requests
        ResetGame();
        BroadcastStartGame();
        RollDice();
    }



    // ----- Outgoing RPCs:
    // This RPC is called when a client joins who is a player:
    void SendPrivateInformationCommand(int playerID, TcpNetworkConnection connection) {
		OSCMessageOut message = new OSCMessageOut("/PlayerInfo").AddInt(playerID);
		connection.Send(message.GetBytes()); // private message
	}

    void BroadcastDiceRolled(int d1, int d2)
    {
        OSCMessageOut msg = new OSCMessageOut("/DiceRolled")
            .AddInt(d1)
            .AddInt(d2);

        Broadcast(msg.GetBytes());
    }

    void BroadcastGridUpdate(int player, int col)
    {
        Model m;
        if (player == 0)
        {
            m = p1Model;

        }
        else
        {
            m = p2Model;
        }


        for (int row = 0; row < 3; row++)
        {
            int value = m.grid[row, col];
            OSCMessageOut msg = new OSCMessageOut("/GridUpdated")
                .AddInt(player)
                .AddInt(row)
                .AddInt(col)
                .AddInt(value);

            Broadcast(msg.GetBytes());
        }
    }
    void BroadcastScoreUpdate()
    {
        int s1 = p1Model.CalculateGridScore();
        int s2 = p2Model.CalculateGridScore();

        OSCMessageOut msg = new OSCMessageOut("/ScoreUpdated")
            .AddInt(s1)
            .AddInt(s2);

        Broadcast(msg.GetBytes());
    }

    void BroadcastTurnChange(int player)
    {
        OSCMessageOut msg = new OSCMessageOut("/TurnChanged")
            .AddInt(player);

        Broadcast(msg.GetBytes());
    }

    public void ActivePlayerChangeRpc(int player) {
		OSCMessageOut message = new OSCMessageOut("/ActivePlayer").AddInt(player);
		Broadcast(message.GetBytes());
	}
    void BroadcastStartGame()
    {
        OSCMessageOut msg = new OSCMessageOut("/StartGame");
        Broadcast(msg.GetBytes());
    }

    public void GameOverRpc(int winner) {
		OSCMessageOut message = new OSCMessageOut("/GameOver").AddInt(winner);
		Broadcast(message.GetBytes());
	}
	void Broadcast(byte[] packet) {
		foreach (var conn in connections) {
			conn.Send(packet);
		}
	}

    private void Log( string message)
    {
        if (showDebugMessages) {
            Debug.Log(message);
        }
    }
}
