using System.Collections.Generic;
using UnityEngine;

public class View : MonoBehaviour
{
    [SerializeField] private List<GameObject> dicePrefabs;
    [SerializeField] private GridTransform player1Grid;
    [SerializeField] private GridTransform player2Grid;
    [SerializeField] private List<Transform> rolledDiceTransform;
    [SerializeField] private Controller controller;
    [SerializeField] private GameManager gameManager;

    private int[] diceValueArray;

    private void Start()
    {   
        ShowRolledDice();
    }

    private void ShowRolledDice()
    {
        diceValueArray = controller.RollDice();
        for (int i = 0; i < diceValueArray.Length; i++)
        {
            int diceValue = diceValueArray[i];
            GameObject dice = Instantiate(dicePrefabs[diceValue - 1], rolledDiceTransform[i].position, dicePrefabs[diceValue - 1].transform.rotation, rolledDiceTransform[i]);
            dice.GetComponent<Selectable>().index = i + 1; 
        }
    }

    public void UpdateGrid(int gridIndex, int row, int col, int diceValue)
    {
        GridTransform grid;
        if(gridIndex == 1)
        {
            grid = player1Grid;       
        }
        else 
        {
            grid=player2Grid;
        }
        Transform cellTransform = grid.cols[col].rows[row];
        if(cellTransform.childCount > 0)
        {
            print("A dice on " + cellTransform + " position with a value " + diceValue + " has been deleted from " + grid.name + " grid.");
            Destroy(cellTransform.GetChild(0).gameObject);
        }
        if(diceValue > 0)
        {
            print("A dice on "+cellTransform+" position with a value "+diceValue+" has been added to "+ grid.name+" grid.");
            GameObject die =Instantiate(dicePrefabs[diceValue - 1], cellTransform);
            die.GetComponent<Selectable>().enabled = false;
        }
        ShowRolledDice();
    }
    
    
}
