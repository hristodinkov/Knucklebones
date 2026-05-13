using Unity.VisualScripting;
using UnityEngine;

public class SelectedDiceVisual : MonoBehaviour
{
    public Material normalMat;
    public Material selectedMat;

    private Selectable currentSelected;
    [SerializeField] private SelectColumn selectColumn;
    [SerializeField] private Controller controller;

    public void SetSelected(Selectable selectable)
    { 
        if (currentSelected != null)
        {
            var prevRenderer = currentSelected.GetComponent<MeshRenderer>();
            if (prevRenderer != null)
                prevRenderer.material = normalMat;
        }

        currentSelected = selectable;

        if (currentSelected != null)
        {
            if(controller.LocalPlayer==controller.ActivePlayer)
            {
                var renderer = currentSelected.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.material = selectedMat;
                selectColumn.diceIschosen = true;
            } 
        }
    }
}
