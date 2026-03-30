using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;


public class GameManager : MonoBehaviour 
{
    public Model player1Data = new Model(1);
    public Model player2Data = new Model(2);

    private bool turnOrder =true;

    private int selectedDiceValue;

    private int diceValue1;
    private int diceValue2;

    [SerializeField] private Controller controller;
    [SerializeField] private View view;
    
    private void Start()
    {
        player1Data.OnGridUpdated+=view.UpdateGrid;
        player2Data.OnGridUpdated+=view.UpdateGrid;
    }
    private void Update()
    {
        //ConsoleTest();

    }
    private void OnDestroy()
    {
        player1Data.OnGridUpdated-=view.UpdateGrid;
        player2Data.OnGridUpdated-=view.UpdateGrid; 
    }

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

    public void ChooseCol(int selectedCol)
    {
        if (selectedDiceValue == 0)
        {
            Debug.LogWarning("Please select a die!");
            return;
        }
        if (turnOrder)
        {
            PlaceDice(player1Data, player2Data,selectedCol);
        }
        else
        {
            PlaceDice(player2Data, player1Data,selectedCol);
        }
    }

    private void PlaceDice(Model gridForAdd, Model gridForRemove,int selectedCol)
    {
        bool succeed = gridForAdd.TryAddNewDice(selectedDiceValue, selectedCol);
        if (!succeed)
        {
            Debug.LogWarning("Col full. Try another one.");
            return;
        }
        gridForRemove.TryRemoveNumber(selectedCol, selectedDiceValue);
        print("Player 1 has " + player1Data.CalculateGridScore() + " points");
        print("Player2 has " + player2Data.CalculateGridScore() + " points");
        player1Data.PrintGrid();
        player2Data.PrintGrid();
        selectedDiceValue = 0;
        turnOrder = !turnOrder;
    }

    #region ConsoleTest
    private void ConsoleTest()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            SelectDice(1);
        }
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            SelectDice(2);
        }
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            ChooseColConsole(0);
        }
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            ChooseColConsole(1);
        }
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            ChooseColConsole(2);
        }
    }

    
    public void ChooseColConsole(int selectedCol)
    {
        if (turnOrder)
        {
            bool succeed = player1Data.TryAddNewDice(selectedDiceValue, selectedCol);
            if(!succeed)
            {
                Debug.LogWarning("Col full. Try another one.");
                return;
            }
            player2Data.TryRemoveNumber(selectedCol, selectedDiceValue);
            
            print("Player 1 has " + player1Data.CalculateGridScore() + " points");
            print("Player2 has " + player2Data.CalculateGridScore() + " points");
            turnOrder = false;
        }
        else
        {
            bool succeed = player2Data.TryAddNewDice(selectedDiceValue, selectedCol );
            if (!succeed)
            {
                Debug.LogWarning("Col full. Try another one.");
                return;
            }
            player1Data.TryRemoveNumber(selectedCol, selectedDiceValue);
            print("Player 1 has " + player1Data.CalculateGridScore() + " points");
            print("Player2 has " + player2Data.CalculateGridScore() + " points");
            turnOrder = true;
        }
        player1Data.PrintGrid();
        player2Data.PrintGrid();

        RollDice();
    }
    #endregion

}
