using UnityEngine;

public class GridTransform : MonoBehaviour
{
    public TransformRow[] cols;

    public Transform[,] To2DArray()
    {
        int height = cols.Length;
        int width = cols[0].rows.Length;

        Transform[,] result = new Transform[height, width];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                result[y, x] = cols[y].rows[x];

        return result;
    }
}

[System.Serializable]
public class TransformRow
{
    public Transform[] rows;
}
