using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;


public class GameManager : MonoBehaviour
{

    [SerializeField] private View view;
    [SerializeField] private Client client;

    private bool turnOrder = true;

    private int selectedDiceValue;
    private int diceValue1;
    private int diceValue2;

    private Player1ClientModel p1Client;
    private Player2ClientModel p2Client;

    private Player1ViewModel p1ViewModel;
    private Player2ViewModel p2ViewModel;

    private void Start()
    {
        p1Client = new Player1ClientModel();
        p2Client = new Player2ClientModel();

        p1ViewModel = new Player1ViewModel(client, p1Client, view);
        p2ViewModel = new Player2ViewModel(client, p2Client, view);

        view.Initialize(client);
        //uiView.Initialize(client);
    }

    #region ConsoleTest
    //private void ConsoleTest()
    //{
    //    if (Keyboard.current.qKey.wasPressedThisFrame)
    //    {
    //        SelectDice(1);
    //    }
    //    if (Keyboard.current.wKey.wasPressedThisFrame)
    //    {
    //        SelectDice(2);
    //    }
    //    if (Keyboard.current.aKey.wasPressedThisFrame)
    //    {
    //        ChooseColConsole(0);
    //    }
    //    if (Keyboard.current.sKey.wasPressedThisFrame)
    //    {
    //        ChooseColConsole(1);
    //    }
    //    if (Keyboard.current.dKey.wasPressedThisFrame)
    //    {
    //        ChooseColConsole(2);
    //    }
    //}


    //public void ChooseColConsole(int selectedCol)
    //{
    //    if (turnOrder)
    //    {
    //        bool succeed = player1Data.TryAddNewDice(selectedDiceValue, selectedCol);
    //        if(!succeed)
    //        {
    //            Debug.LogWarning("Col full. Try another one.");
    //            return;
    //        }
    //        player2Data.TryRemoveNumber(selectedCol, selectedDiceValue);

    //        print("Player 1 has " + player1Data.CalculateGridScore() + " points");
    //        print("Player2 has " + player2Data.CalculateGridScore() + " points");
    //        turnOrder = false;
    //    }
    //    else
    //    {
    //        bool succeed = player2Data.TryAddNewDice(selectedDiceValue, selectedCol );
    //        if (!succeed)
    //        {
    //            Debug.LogWarning("Col full. Try another one.");
    //            return;
    //        }
    //        player1Data.TryRemoveNumber(selectedCol, selectedDiceValue);
    //        print("Player 1 has " + player1Data.CalculateGridScore() + " points");
    //        print("Player2 has " + player2Data.CalculateGridScore() + " points");
    //        turnOrder = true;
    //    }
    //    player1Data.PrintGrid();
    //    player2Data.PrintGrid();

    //    RollDice();
    //}

    //public bool TryPlaceDice(int selectedCol)
    //{
    //    if (selectedDiceValue == 0)
    //    {
    //        Debug.LogWarning("Please select a die!");
    //        return false;
    //    }

    //    Model add;
    //    Model remove;
    //    if (turnOrder)
    //    {
    //        add = player1Data;
    //        remove = player2Data;
    //    }
    //    else
    //    {
    //        add = player2Data;
    //        remove = player1Data;
    //    }

    //    bool succeed = add.TryAddNewDice(selectedDiceValue, selectedCol);
    //    if (!succeed)
    //    {
    //        Debug.LogWarning("Col full. Try another one.");
    //        return false;
    //    }

    //    remove.TryRemoveNumber(selectedCol, selectedDiceValue);

    //    print("Player 1 has " + player1Data.CalculateGridScore() + " points");
    //    print("Player2 has " + player2Data.CalculateGridScore() + " points");
    //    player1Data.PrintGrid();
    //    player2Data.PrintGrid();

    //    selectedDiceValue = 0;
    //    turnOrder = !turnOrder;
    //    return true;
    //}

    public int[] RollDice()
    {
        diceValue1 = Random.Range(1, 7);
        diceValue2 = Random.Range(1, 7);
        return new int[] { diceValue1, diceValue2 };
        //print("The rolled dices are "+ diceValue1+" and " + diceValue2+ " \nChoose which one you would like to use. Q for the first one and W for the second one");
    }

    public void SelectDice(int selectedDice)
    {
        if (selectedDice == 1)
        {
            selectedDiceValue = diceValue1;
        }
        else
        {
            selectedDiceValue = diceValue2;
        }
        print("You selected " + selectedDiceValue + "\nNow select which col you want to put your die. First is A, second S and third D");
    }
    #endregion

}
