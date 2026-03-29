using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class TerrainStats : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI terrainHeight;
    [SerializeField]
    WFC WFC;
    void Start()
    {
        StartCoroutine(wait());
        
    }

    private IEnumerator wait()
    {
        yield return new WaitForSeconds(2);
        terrainHeight = GetComponent<TextMeshProUGUI>();

        terrainHeight.text = CalculateAvgHeight().ToString();
    }

    private float CalculateAvgHeight()
    {
        Cell[,,] grid = WFC.GetGrid();
        int heightSum = 0;
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                for (int k = 0; k < grid.GetLength(2); k++)
                {
                    Cell cell = grid[i, j, k];
                    if (cell.possibleModules[0].name.ToLower() == "air")
                    {
                        heightSum++;
                    }
                }
            }   
        }
        //Debug.Log("HEIGHTSUM: " + heightSum);
        //Debug.Log("GRIDLength: 0" + grid.GetLength(0));
        //Debug.Log("GRIDLength: 1" + grid.GetLength(1));
        //Debug.Log("GRIDLength: 2" + grid.GetLength(2));
        return (((float)grid.GetLength(0) * grid.GetLength(1) * grid.GetLength(2)) - heightSum)/((float)grid.GetLength(0) * grid.GetLength(2));
    }

    private void Update()
    {
        
    }
}
