using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ModulData", menuName = "Scriptable Objects/ModulData")]
public class ModulData : ScriptableObject
{
    public string name;
    public GameObject prefab;
    public string pX, nX, pY, nY, pZ, nZ;
    public int weight;
    public string constrain_to, constrain_from;
    // 6 entries, one per face direction
    public List<string>[] valid_neighbours = new List<string>[6];
    public int mesh_rotation;

    public static readonly string[] SideOrder = { "pX", "nX", "pY", "nY", "pZ", "nZ" };

    public List<string> GetValidNeighbours(string side)
    {
        int index = System.Array.IndexOf(SideOrder, side);

        if (index < 0)
        {
            Debug.LogError($"Unknown side '{side}' on module '{name}'. SideOrder is: {string.Join(", ", SideOrder)}");
            return new List<string>();
        }
        if (valid_neighbours[index] == null)
        {
            Debug.LogError($"valid_neighbours[{index}] is null on module '{name}'");
            return new List<string>();
        }

        return valid_neighbours[index];
    }

}