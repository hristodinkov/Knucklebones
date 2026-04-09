using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private View view;
    private void Start()
    {
        RollDice();
    }
    public void ChooseDice(int diceIndex)
    {
        gameManager.SelectDice(diceIndex);
    }
    public void ChooseCol(int colIndex)
    {
        if (gameManager.TryPlaceDice(colIndex))
        {
            RollDice();
        }
    }

    private void RollDice()
    {
        int[] dice = gameManager.RollDice();
        view.ShowRolledDice(dice);
    }

}
