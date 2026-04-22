using UnityEngine;

public class GridViewModel
{
    private LocalClientModel localClientModel;
    private View view;

    public GridViewModel(Client client,LocalClientModel localClientModel,View view,int playerIndex)
    {
        this.localClientModel = localClientModel;
        this.view = view;
        client.OnGridUpdated += (player, row, col, value) =>
        {
            if (player == playerIndex)
            {
                HandleGridUpdated(row, col, value);
            }
        };
    }

    protected virtual void HandleGridUpdated( int row, int col, int value)
    {
        localClientModel.values[row, col] = value;
        view.RenderCell(localClientModel,row, col);
    }
}
