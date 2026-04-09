using UnityEngine;

public class GridViewModel
{
    private LocalClientModel localClientModel;
    private View view;

    public GridViewModel(Model model,LocalClientModel localClientModel,View view)
    {
        this.localClientModel = localClientModel;
        this.view = view;
        model.OnGridUpdated += HandleGridUpdated;
    }

    protected virtual void HandleGridUpdated( int row, int col, int value)
    {
        localClientModel.values[row, col] = value;
        view.RenderCell(localClientModel,row, col);
    }
}
