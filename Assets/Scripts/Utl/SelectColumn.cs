using Unity.VisualScripting;
using UnityEngine;

public class SelectColumn : MonoBehaviour
{
    [SerializeField] private Material yellowMaterial;
    [SerializeField] private Material whiteMaterial;

    [SerializeField] private GameObject[] columnObjectsPlayer1;
    [SerializeField] private GameObject[] columnObjectsPlayer2;

    [HideInInspector] public bool diceIschosen;
    [SerializeField] private Controller controller;

    private float time;

    // Update is called once per frame
    void Update()
    {
        if (diceIschosen)
        {
            if(controller.ActivePlayer == 0)
            {
                foreach (GameObject column in columnObjectsPlayer1)
                {
                    var renderer = column.GetComponent<MeshRenderer>();
                    if (renderer != null&& renderer.material != yellowMaterial&&time<5.0f)
                    {
                        renderer.material = yellowMaterial;
                        time += Time.deltaTime;
                    }
                    else
                    {
                        renderer.material = whiteMaterial;
                        time += Time.deltaTime;
                    }
                    if(time>=10.0f)
                    {
                        time = 0.0f;
                    }
                        
                }
            }
            else
            {
                foreach (GameObject column in columnObjectsPlayer2)
                {
                    var renderer = column.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.material != yellowMaterial && time < 5.0f)
                    { 
                        renderer.material = yellowMaterial; 
                        time += Time.deltaTime; 
                    }
                    else
                    {
                        renderer.material = whiteMaterial;
                        time += Time.deltaTime;
                    }
                    if (time >= 10.0f)
                    {
                        time = 0.0f;
                    }
                }
            }
        }
        else
        {
            ResetColumns();
        }
    }

    public void ResetColumns()
    {
        foreach (GameObject column in columnObjectsPlayer1)
        {
            var renderer = column.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.material = whiteMaterial;
        }
        foreach (GameObject column in columnObjectsPlayer2)
        {
            var renderer = column.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.material = whiteMaterial;
        }
    }
}
