using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;

public class Selectable : MonoBehaviour
{
    public int index;
    public bool isDice = false;
    public bool isCol = false;
    [SerializeField] private Controller controller;

    private void Start()
    {
        controller = FindAnyObjectByType<Controller>();
    }
    public void OnPointerClick()
    {
        if (isDice)
        {
            print("You selected dice on index " + index);
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
