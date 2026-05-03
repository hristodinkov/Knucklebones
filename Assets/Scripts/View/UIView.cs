using System;
using TMPro;
using UnityEngine;

public class UIView : MonoBehaviour
{
    private int scoreP1;
    private int scoreP2;

    private int winner;
    private bool isGameOver;

    private int activePlayer;

    [SerializeField] private Controller controller;
    [SerializeField] private Client client;

    [SerializeField] private float areaXValue= 0.02f;
    [SerializeField] private float areaYValue= 0.02f;
    [SerializeField] private float areaHValue= 0.6f;
    [SerializeField] private float areaWValue= 0.25f;
    [SerializeField] private float fontSizeValue = 0.02f;
    void Start()
    {
        client.OnStartGame += ResetUI;
        client.OnScoreUpdated += HandleScoreUpdated;
        client.OnTurnChanged += HandleTurnChanged;
        client.OnGameOver += HandleGameOver;
    }

    void HandleScoreUpdated(int s1, int s2)
    {
        scoreP1 = s1;
        scoreP2 = s2;
    }

    void HandleTurnChanged(int p)
    {
        activePlayer = p;
    }

    void HandleGameOver(int winner) 
    {
        isGameOver = true;
        this.winner = winner;
        Debug.Log("Game Over! Winner: Player " + (winner + 1));
    }

    void OnGUI()
    {
        float areaX = Screen.width * areaXValue;
        float areaY = Screen.height * areaYValue;
        float areaW = Screen.width * areaWValue;
        float areaH = Screen.height * areaHValue;

        int fontSize = Mathf.RoundToInt(Screen.height * fontSizeValue);
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = fontSize;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = fontSize;

        GUILayout.BeginArea(new Rect(areaX, areaY, areaW, areaH), GUI.skin.box);

        GUILayout.Space(10);

        GUILayout.Label("Scores:", labelStyle);
        GUILayout.Label("Player 2: " + scoreP2, labelStyle);
        GUILayout.Label("Player 1: " + scoreP1, labelStyle);
        

        GUILayout.Space(10);

        GUILayout.Label("Current Turn: Player " + (activePlayer + 1), labelStyle);
        GUILayout.EndArea();

        if (isGameOver)
        {
            GUILayout.BeginArea(new Rect(Screen.width * 0.5f, Screen.height * 0.5f, Screen.width * 0.4f, Screen.height * 0.3f), GUI.skin.box);
            GUILayout.Label("Game Over!\n  Winner: Player " + (winner + 1), labelStyle);
            if (GUILayout.Button("Play Again", buttonStyle))
            {
                controller.SendRematchRequest();
            }
            if (GUILayout.Button("Quit", buttonStyle))
            {
                Application.Quit();
            }
            GUILayout.EndArea();
        }
    }

    public void ResetUI()
    {
        isGameOver = false;
        winner = -1;

        scoreP1 = 0;
        scoreP2 = 0;
    }

}
