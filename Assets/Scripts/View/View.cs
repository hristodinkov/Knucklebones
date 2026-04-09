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

    private void Awake()
    {
        diceValueArray = new int[2];
    }

    public void ShowRolledDice(int[] diceValues)
    {
        diceValueArray = diceValues;
        foreach (Transform item in rolledDiceTransform)
        {
            if (item.childCount > 0)
            {
                Destroy(item.GetChild(0).gameObject);
            }
        }
        for (int i = 0; i < diceValueArray.Length; i++)
        {
            int diceValue = diceValueArray[i];
            GameObject dice = Instantiate(dicePrefabs[diceValue - 1], rolledDiceTransform[i]);
            dice.GetComponent<Selectable>().index = i + 1; 
            dice.GetComponent<Selectable>().isDice = true;
        }
    }

    public void RenderCell(LocalClientModel client, int row, int col)
    {
        int value = client.values[row, col];
        Transform cell = GetCellTransform(client, row, col);

        if (client.diceObjects[row, col] != null)
        {
            Destroy(client.diceObjects[row, col]);
            client.diceObjects[row, col] = null;
        }

        if (value > 0)
        {
            GameObject die = Instantiate(dicePrefabs[value - 1], cell);
            Selectable selectable = die.GetComponent<Selectable>();
            if(selectable != null)
            {
                selectable.enabled = false;
            }
            client.diceObjects[row, col] = die;
        }
    }
    private Transform GetCellTransform(LocalClientModel client, int row, int col)
    {
        if (client is Player1ClientModel)
            return player1Grid.cols[col].rows[row];

        return player2Grid.cols[col].rows[row];
    }
}
