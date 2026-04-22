using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Client client;
    [SerializeField] private View view;
    private void Start()
    {
        //RollDice();
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

    //private void RollDice()
    //{
    //    int[] dice = gameManager.RollDice();
    //    view.ShowRolledDice(dice);
    //}

}
