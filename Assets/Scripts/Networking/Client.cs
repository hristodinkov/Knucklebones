using NetworkConnections;
using OSCTools;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// The client is the class that lets game code (Controller and View classes) communicate with 
/// the server, and handles network connections.
/// </summary>
public class Client : MonoBehaviour
{
	// ----- General client things:
	public IPAddress ServerIP = IPAddress.Loopback;
	TcpNetworkConnection connection;
	OSCDispatcher dispatcher;


	public event System.Action<int> OnPlayerInfoReceived;

    public event Action<int, int> OnDiceRolled;
    public event Action<int, int, int,int> OnGridUpdated;
    public event Action<int, int> OnScoreUpdated;
    public event Action<int> OnTurnChanged;

    void Start()
    {
		TcpClient client = new TcpClient();
        try
        {
            client.Connect(new IPEndPoint(ServerIP, 50006));
            connection = new TcpNetworkConnection(client);
        }
        catch (SocketException e)
        {
            Debug.LogError("Client: Could not connect to server: " + e.Message);
            enabled = false;
            return;
        }
        // TODO: error handling

        Debug.Log("Starting client, connecting to " + ServerIP);

		// Initialize the dispatcher and callbacks for incoming OSC messages:
		dispatcher = new OSCDispatcher();
		dispatcher.ShowIncomingMessages = true;
		Initialize();
    }

	/// <summary>
	/// Called from NetworkConnection callback (connection.Update), when a packet arrives:
	/// </summary>
	void HandlePacket(byte[] packet, IPEndPoint remote) {
		OSCMessageIn mess = new OSCMessageIn(packet);
		Debug.Log("Message arrives on client: " + mess);
		dispatcher.HandlePacket(packet, remote);
	}

	void Update()
    {

        if (connection == null)
            return;

        // Check for incoming packets, and deal with them:
        while (connection.Available()>0) {
			HandlePacket(connection.GetPacket(), connection.Remote);
		}
		// TODO: disconnect handling
    }

	void Initialize() {
        // The (optional) list of parameter types (OSCUtil.INT) lets the dispatcher filter
        //  messages that do not satisfy the expected signature (=parameter list): 
        //dispatcher.AddListener("/ActivePlayer", ActivePlayerChangeRpc, OSCUtil.INT);
        //dispatcher.AddListener("/GameOver", GameOverRpc, OSCUtil.INT);
        dispatcher.AddListener("/PlayerInfo", PlayerInfoRpc, OSCUtil.INT);

        dispatcher.AddListener("/DiceRolled", DiceRolledRpc, OSCUtil.INT, OSCUtil.INT);
        dispatcher.AddListener("/GridUpdated", GridUpdatedRpc, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT);
        dispatcher.AddListener("/ScoreUpdated", ScoreUpdatedRpc, OSCUtil.INT, OSCUtil.INT);
        dispatcher.AddListener("/TurnChanged", TurnChangedRpc, OSCUtil.INT);
    }

    // ----- Incoming RPCs (events are triggered, and View classes subscribe):

    void DiceRolledRpc(OSCMessageIn msg, IPEndPoint remote)
    {
        int d1 = msg.ReadInt();
        int d2 = msg.ReadInt();
        OnDiceRolled?.Invoke(d1, d2);
    }

    void GridUpdatedRpc(OSCMessageIn msg, IPEndPoint remote)
    {
        int player = msg.ReadInt();
        int row = msg.ReadInt();
        int col = msg.ReadInt();
        int value = msg.ReadInt();

        Debug.Log($"GridUpdatedRpc: {player} -> [{col},{row}] = {value}" );
        OnGridUpdated?.Invoke(player, row, col, value);
    }

    void ScoreUpdatedRpc(OSCMessageIn msg, IPEndPoint remote)
    {
        int p1 = msg.ReadInt();
        int p2 = msg.ReadInt();
        OnScoreUpdated?.Invoke(p1, p2);
    }

    void TurnChangedRpc(OSCMessageIn msg, IPEndPoint remote)
    {
        int currentPlayer = msg.ReadInt();
        OnTurnChanged?.Invoke(currentPlayer);
    }

    //void GameOverRpc(OSCMessageIn message, IPEndPoint remote) {
    //	int winner = message.ReadInt();
    //	OnGameOver?.Invoke(winner);
    //}
    void PlayerInfoRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int playerIndex = message.ReadInt();
		OnPlayerInfoReceived?.Invoke(playerIndex);
	}

    // ----- Outgoing RPCs (called from Controller):

    public void SendChooseDice(int diceIndex)
    {
        OSCMessageOut message = new OSCMessageOut("/ChooseDice").AddInt(diceIndex);
        connection.Send(message.GetBytes());
    }

    public void SendChooseColumn(int colIndex)
    {
        OSCMessageOut message = new OSCMessageOut("/ChooseColumn").AddInt(colIndex);
        connection.Send(message.GetBytes());
    }



}
