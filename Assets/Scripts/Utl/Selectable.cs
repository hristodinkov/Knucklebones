using UnityEngine;
using UnityEngine.EventSystems;

public class Selectable : MonoBehaviour
{
    public int index;
    public bool isDice = false;
    public bool isCol = false;
    [SerializeField] private Controller controller;
    [SerializeField] private SelectedDiceVisual diceVisual;

    private void Start()
    {
        controller = FindAnyObjectByType<Controller>();
        diceVisual = FindAnyObjectByType<SelectedDiceVisual>();
    }
    public void OnPointerClick()
    {
        if (isDice)
        {
            print("You selected dice on index " + index);
            diceVisual.SetSelected(this);
            controller.ChooseDice(index);
        }
        else if (isCol)
        {
            print("You selected column: " + index);
            controller.ChooseCol(index);    
        }
        else
        {
            print("This object is not selectable.");
        }
    }
}
