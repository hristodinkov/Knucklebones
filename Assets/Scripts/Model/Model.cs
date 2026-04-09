using UnityEngine;
using System;

public class Model
{
    public event Action<int, int, int> OnGridUpdated; // params: row, col, diceValue 

    public int[,] grid = new int [3, 3];

    public int gridIndex; 

    public Model(int gridIndex)
    {
        this.gridIndex = gridIndex;
    }

    public bool TryAddNewDice(int diceValue,int col)
    {   
        for (int row = 0; row < grid.GetLength(0); row++)
        {
            if (grid[row, col] == 0)
            {
                grid[row, col] = diceValue;  

                Debug.Log("A new dice have been added: "+diceValue+" as value , on row and col"+ row +" , " + col);
                OnGridUpdated?.Invoke(row, col, diceValue);
                CalculateGridScore();
                return true;
            }
        }
        return false;
    }

    public int CalculateColScore(int col)
    {
        int row1 = grid[0,col];
        int row2 = grid[1,col];
        int row3 = grid[2,col];
        int sum = row1 + row2 + row3;

        if (sum==0)
        {
            return 0;
        }
        else if(row1 == row2 && row2 == row3)
        {
            return sum*3;
        }
        else if(row1==row2)
        {
            return (row1+row2)*2 + row3;
        }
        else if(row2 == row3)
        {
            return row1 + (row2+row3)*2;
        }
        else if(row1 == row3)
        {
            return (row1+row3)*2 + row2;
        }
        else
        {
            return sum;
        }

    }

    public int CalculateGridScore()
    {
        int totalScore = 0;
        for (int col = 0; col < grid.GetLength(1); col++)
        {
            totalScore += CalculateColScore(col);
        }
        return totalScore;
    }

    public void TryRemoveNumber(int col, int number)
    {
        for (int row = 0; row < grid.GetLength(0); row++)
        {
            if (grid[row, col] == number)
            {
                grid[row, col] = 0;
                OnGridUpdated?.Invoke(row, col, 0); 
            }
        }
        CalculateGridScore();
    }

    public void PrintGrid()
    {
        Debug.Log(grid[0,0] + " " + grid[0, 1] + " " + grid[0, 2] + "\n"+
                 grid[1, 0] + " " + grid[1, 1] + " " + grid[1, 2] + "\n"+
                 grid[2, 0] + " " + grid[2, 1] + " " + grid[2, 2] + "\n" );
    }

}
