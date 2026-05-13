using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Client client;
    [SerializeField] private View view;
    [SerializeField] private UIView uIView;
    private int localPlayer;
    public int LocalPlayer => localPlayer;
    private int activePlayer;
    public int ActivePlayer => activePlayer;

    private void Start()
    {
        client.OnPlayerInfoReceived += HandleLocalPlayerInfo;
        client.OnTurnChanged += HandleActivePlayer;
    }
    //public void ChooseDice(int diceIndex)
    //{
    //    gameManager.SelectDice(diceIndex);
    //}
    //public void ChooseCol(int colIndex)
    //{
    //    if (gameManager.TryPlaceDice(colIndex))
    //    {
    //        RollDice();
    //    }
    //}

    public void ChooseDice(int diceIndex)
    {
        client.SendChooseDice(diceIndex);
    }

    public void ChooseCol(int colIndex)
    {
        client.SendChooseColumn(colIndex);
    }
    public void SendRematchRequest()
    {
        client.SendRematchRequest();
    }
    public void HandleLocalPlayerInfo(int playerIndex)
    {
        localPlayer = playerIndex;
    }
    public void HandleActivePlayer(int playerIndex)
    {
        activePlayer = playerIndex;
    }
    public void LeaveMatch()
    {
        client.SendLeaveRoom();
    }
    public void Quit()
    {
        Application.Quit();
    }

    //private void RollDice()
    //{
    //    int[] dice = gameManager.RollDice();
    //    view.ShowRolledDice(dice);
    //}

}
