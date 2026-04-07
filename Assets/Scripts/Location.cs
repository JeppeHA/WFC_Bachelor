using UnityEngine;

public class Location : MonoBehaviour
{
    public string name;
    public MapNode[] neighbors = new MapNode[4];
    public int[] directions = new int[4];
    public GameObject map;
}
