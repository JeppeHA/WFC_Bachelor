using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a single cell in the WFC grid.
/// Tracks which tile options are still possible (the "superposition").
/// </summary>
public class WFCCell
{
    public List<int> possibleModules; // Indices into the tile list
    public bool collapsed = false;
    public int collapsedModuleIndex = -1;

    public WFCCell(int totalTiles)
    {
        possibleModules = Enumerable.Range(0, totalTiles).ToList();
    }

    /// <summary>Shannon entropy weighted by tile weights.</summary>
    public float Entropy(WFCModule[] tiles)
    {
        float totalWeight = possibleModules.Sum(i => tiles[i].weight);
        float entropy = 0f;
        foreach (int i in possibleModules)
        {
            float p = tiles[i].weight / totalWeight;
            if (p > 0f) entropy -= p * UnityEngine.Mathf.Log(p);
        }
        return entropy;
    }

    public bool IsContradiction => possibleModules.Count == 0 && !collapsed;
}
