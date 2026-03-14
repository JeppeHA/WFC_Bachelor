using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ModulData", menuName = "Scriptable Objects/ModulData")]
public class ModulData : ScriptableObject
{
    public string name;
    public GameObject prefab;
    public string pX, nX, pY, nY, pZ, nZ;
    public int weight;

    // 6 entries, one per face direction
    public List<string>[] valid_neighbours = new List<string>[6];

    public static readonly string[] SideOrder = { "pX", "nX", "pY", "nY", "pZ", "nZ" };

    public List<string> GetValidNeighbours(string side)
    {
        int index = System.Array.IndexOf(SideOrder, side);
        return index >= 0 ? valid_neighbours[index] : new List<string>();
    }
}