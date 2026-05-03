using System.Collections.Generic;
using UnityEngine;

public class View : MonoBehaviour
{
    [SerializeField] private List<GameObject> dicePrefabs;
    [SerializeField] private GridTransform player1Grid;
    [SerializeField] private GridTransform player2Grid;
    [SerializeField] private List<Transform> rolledDiceTransform;
    //[SerializeField] private Controller controller;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Client client;

    private int[] diceValueArray;

    private void Awake()
    {
        diceValueArray = new int[2];
        client.OnStartGame += ClearBoard;
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
            dice.GetComponent<Selectable>().index = i; 
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
    private Transform GetCellTransform(LocalClientModel model, int row, int col)
    {
        if (model == client.p1Model)
            return player1Grid.cols[col].rows[row];

        return player2Grid.cols[col].rows[row];
    }

    public void ClearBoard()
    {
        foreach (Transform t in rolledDiceTransform)
        {
            if (t.childCount > 0)
                Destroy(t.GetChild(0).gameObject);
        }

        ClearGrid(client.p1Model);
        ClearGrid(client.p2Model);
    }

    private void ClearGrid(LocalClientModel model)
    {
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            { 
                if (model.diceObjects[r, c] != null)
                {
                    Destroy(model.diceObjects[r, c]);
                    model.diceObjects[r, c] = null;
                }
                model.values[r, c] = 0;
                ClearCell(player1Grid.cols[c].rows[r]);
                ClearCell(player2Grid.cols[c].rows[r]);
            }
        }
    }
    private void ClearCell(Transform cell)
    {
        if (cell.childCount > 0)
        {
            Destroy(cell.GetChild(0).gameObject);
        }
    }



    public void Initialize()
    {
        client.OnDiceRolled += (d1, d2) =>
        {
            ShowRolledDice(new int[] { d1, d2 });
        };
        client.OnGridUpdated += (playerId, row, col, value) =>
        {
            if ((playerId == 0 && client.p1Model != null) || (playerId == 1 && client.p2Model != null))
            {
                LocalClientModel model;
                if (playerId == 0)
                {
                    model = client.p1Model;
                }
                else
                {
                    model = client.p2Model;
                }
                RenderCell(model, row, col);
            }
        };
        
    }

}
