using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private View view;
    public int[] RollDice() 
    {
        return gameManager.RollDice();
    }
    public void ChooseDice(int diceIndex)
    {
        gameManager.SelectDice(diceIndex);
    }
    public void ChooseCol(int colIndex)
    {
        gameManager.ChooseCol(colIndex);
    }
    

}
