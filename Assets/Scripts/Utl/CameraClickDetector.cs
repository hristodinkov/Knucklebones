using UnityEngine;
using UnityEngine.InputSystem;

public class CameraClickDetector : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if(hit.collider.gameObject.TryGetComponent(out Selectable selectable))
                {
                    selectable.OnPointerClick();
                }
            }
        }
    }
}
