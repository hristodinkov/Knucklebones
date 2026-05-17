using System;
using TMPro;
using UnityEngine;

public class UIView : MonoBehaviour
{
    private int scoreP1;
    private int scoreP2;

    private int winner;
    private bool isGameOver;

    private bool isOpponentDisconnected;

    private int activePlayer;

    [SerializeField] private TextMeshProUGUI scoreP1Text;
    [SerializeField] private TextMeshProUGUI scoreP2Text;
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI timeoutReconnectText;
    [SerializeField] private TextMeshProUGUI moveTimerText;
    [SerializeField] private TextMeshProUGUI tokenStringText;

    [SerializeField] private SessionManager sessionManager;

    [SerializeField] private GameObject gameOverWindow;
    [SerializeField] private TextMeshProUGUI winnerText;

    [SerializeField] private GameObject opponentDisconnectedWindow;
    [SerializeField] private GameObject opponentLeftWindow;

    [SerializeField] private Controller controller;
    [SerializeField] private Client client;

    void Start()
    {
        client.OnStartGame += ResetUI;
        client.OnScoreUpdated += HandleScoreUpdated;
        client.OnTurnChanged += HandleTurnChanged;
        client.OnGameOver += HandleGameOver;
        client.OnOpponentDisconnected += HandleOpponentDisconnected;
        client.OnOpponentReconnected += HandleOpponentReconnected;
        client.OnOpponentLeft += HandleOpponentLeft;
        client.OnReconnectCountdown += HandleReconnectCountdown;
        client.OnMoveCountdown += HandleMoveCountdown;
        client.OnArgsUsed += HandleArgsUsed;
    }

    private void HandleReconnectCountdown(int seconds)
    {
        timeoutReconnectText.text = "Timeout in: " + seconds + " s";
    }
    void HandleMoveCountdown(int seconds)
    {
        moveTimerText.text = "Time to move: " + seconds + "s";
        moveTimerText.color = seconds <= 10 ? Color.red : Color.white;
    }

    void HandleScoreUpdated(int s1, int s2)
    {
        scoreP1 = s1;
        scoreP2 = s2;
        scoreP1Text.text = "Player 1: " + scoreP1;
        scoreP2Text.text = "Player 2: " + scoreP2;
    }

    void HandleTurnChanged(int p)
    {
        activePlayer = p;
        turnText.text = "Turn: Player " + (activePlayer + 1);
    }

    void HandleGameOver(int winner) 
    {
        opponentDisconnectedWindow.SetActive(false);
        opponentLeftWindow.SetActive(false);
        isGameOver = true;
        this.winner = winner;
        winnerText.text = "Player " + (winner + 1)+"wins!";
        gameOverWindow.SetActive(true);
    }

    void HandleOpponentDisconnected() 
    {
        opponentDisconnectedWindow.SetActive(true);
    }
    void HandleOpponentReconnected()
    {
        opponentDisconnectedWindow.SetActive(false);
    }

    void HandleOpponentLeft()
    {
        opponentLeftWindow.SetActive(true);
        opponentDisconnectedWindow.SetActive(false);
        gameOverWindow.SetActive(false);

    }

    void HandleArgsUsed(string token)
    {
        tokenStringText.gameObject.SetActive(true);
        tokenStringText.text = token;
    }

    public void ResetUI()
    {
        gameOverWindow.SetActive(false);
        opponentDisconnectedWindow.SetActive(false);
        opponentLeftWindow.SetActive(false);

        winner = -1;
        isOpponentDisconnected = false;


        scoreP1 = 0;
        scoreP2 = 0;
        scoreP1Text.text = "Player 1: " + scoreP1;
        scoreP2Text.text = "Player 2: " + scoreP2;
        turnText.text = "Turn: Player " + (activePlayer + 1);
        moveTimerText.color = Color.white;
        moveTimerText.text = "Time to move: 90s";
    }

}
