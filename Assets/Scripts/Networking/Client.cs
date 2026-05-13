using NetworkConnections;
using OSCTools;
using System;
using System.ComponentModel;
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

    public LocalClientModel p1Model = new LocalClientModel();
    public LocalClientModel p2Model = new LocalClientModel();

    public int serverPort = 50001;

    [ReadOnly] [SerializeField]private string clientID = string.Empty;

    public event System.Action<int> OnPlayerInfoReceived;

    public event Action<int, int> OnDiceRolled;
    public event Action<int, int, int,int> OnGridUpdated;
    public event Action<int, int> OnScoreUpdated;
    public event Action<int> OnTurnChanged;
    public event Action<int> OnGameOver;
    public event Action OnStartGame;
    public event Action OnOpponentDisconnected;
    public event Action OnOpponentReconnected;


    //------This section is made with MICROSOFT COPILOT------
    private void Awake()
    {
        #if UNITY_EDITOR
        // Editor: always random
        clientID = Guid.NewGuid().ToString();
        #else
            // Build: check for command-line override
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("-id="))
                {
                    clientID = arg.Substring(4);
                    Debug.Log("Using override ClientID: " + clientID);
                    return;
                }
            }

            // Build: fallback to persistent ID
            if (PlayerPrefs.HasKey("ClientID"))
            {
                clientID = PlayerPrefs.GetString("ClientID");
                Debug.Log("Loaded persistent ClientID: " + clientID);
            }
            else
            {
                clientID = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("ClientID", clientID);
                Debug.Log("Generated new persistent ClientID: " + clientID);
            }
        #endif
    }
//-----------------------------------------------------------------------------------

    void OnEnable()
    {
        StartConnection();
		dispatcher = new OSCDispatcher();
		dispatcher.ShowIncomingMessages = true;
		Initialize();
    }

    private void StartConnection()
    {
        TcpClient client = new TcpClient();
        try
        {
            client.Connect(new IPEndPoint(ServerIP, serverPort));
            connection = new TcpNetworkConnection(client);
            // Better: SERVER SIDE: if the ID is empty or wrong, just send a temporary ID (no rejoin possible but eh)
            OSCMessageOut sendCliendID = new OSCMessageOut("/SendClientID").AddString(clientID);
            connection.Send(sendCliendID.GetBytes());
        }
        catch (SocketException e)
        {
            Debug.LogError("Client: Could not connect to server: " + e.Message);
            return;
        }
        Debug.Log("Connected to server at " + ServerIP + ":" + serverPort);
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
        connection.Update();
        while (connection.Available()>0) {
			HandlePacket(connection.GetPacket(), connection.Remote);
		}
    }

    //public void ConnectTo(string ip, int port)
    //{
    //    ServerIP = IPAddress.Parse(ip);
    //    serverPort = port;
    //    Debug.Log($"Connecting to {ip}:{port}");
    //    StartConnection();
    //}

    void Initialize() {
        // The (optional) list of parameter types (OSCUtil.INT) lets the dispatcher filter
        //  messages that do not satisfy the expected signature (=parameter list): 
        dispatcher.AddListener("/PlayerInfo", PlayerInfoRpc, OSCUtil.INT);
        

        dispatcher.AddListener("/DiceRolled", DiceRolledRpc, OSCUtil.INT, OSCUtil.INT);
        dispatcher.AddListener("/GridUpdated", GridUpdatedRpc, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT);
        dispatcher.AddListener("/ScoreUpdated", ScoreUpdatedRpc, OSCUtil.INT, OSCUtil.INT);
        dispatcher.AddListener("/TurnChanged", TurnChangedRpc, OSCUtil.INT);
        dispatcher.AddListener("/GameOver", GameOverRpc, OSCUtil.INT);
        dispatcher.AddListener("/StartGame", StartGameRpc);
        dispatcher.AddListener("/OpponentDisconnected", OpponentDisconnectedRpc);
        dispatcher.AddListener("/OpponentReconnected", OpponentReconnectedRpc);
    }

    // ----- Incoming RPCs (events are triggered, and View classes subscribe):

    void StartGameRpc(OSCMessageIn msg, IPEndPoint remote)
    {
        p1Model = new LocalClientModel();
        p2Model = new LocalClientModel();
        OnStartGame?.Invoke();
    }

    void OpponentDisconnectedRpc(OSCMessageIn msg, IPEndPoint remote)
    {
        OnOpponentDisconnected?.Invoke();
    }

    void OpponentReconnectedRpc(OSCMessageIn msg, IPEndPoint remote)
    {
        OnOpponentReconnected?.Invoke();
    }

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

        LocalClientModel model;
        if(player == 0)
        {
            model = p1Model;
        }
        else
        {
            model = p2Model;
        }
        model.values[row, col] = value;
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

    void GameOverRpc(OSCMessageIn message, IPEndPoint remote)
    {
        int winner = message.ReadInt();
        OnGameOver?.Invoke(winner);
    }
    void PlayerInfoRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int playerIndex = message.ReadInt();
		OnPlayerInfoReceived?.Invoke(playerIndex);
	}

    // ----- Outgoing RPCs (called from Controller):

    public void SendChooseDice(int diceIndex)
    {
        Debug.Log("Client: Sending /ChooseDice with index" + diceIndex);
        OSCMessageOut message = new OSCMessageOut("/ChooseDice").AddInt(diceIndex);
        connection.Send(message.GetBytes());
    }

    public void SendChooseColumn(int colIndex)
    {
        OSCMessageOut message = new OSCMessageOut("/ChooseColumn").AddInt(colIndex);
        connection.Send(message.GetBytes());
    }

    public void SendRematchRequest()
    {
        OSCMessageOut msg = new OSCMessageOut("/RequestRematch");
        connection.Send(msg.GetBytes());
    }

    public void SendLeaveRoom()
    {
        OSCMessageOut msg = new OSCMessageOut("/LeaveRoom");
        connection.Send(msg.GetBytes());
    }


}
