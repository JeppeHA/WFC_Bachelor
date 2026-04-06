using System.Collections.Generic;
using UnityEngine;

//[System.Serializable]
public class MapNode
{
    public MapNode[] neighbors = new MapNode[4]; // fixed size 4, null = no door or unvisited
    //public Transition transition;
    public Dictionary<int, Transition> transitions = new Dictionary<int, Transition>();
    public GameObject map;
    public Vector2Int graphCoord;
    public int[] directions;
    public string name;

    public int AmountOfDoors()
    {
        int doors = 0;
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (neighbors[i] != null)
                doors++;
        }
        return doors;
    }

    public void PrintNeighbors()
    {
        Debug.Log("-----------------------------");
        Debug.Log("Node: " + name);
        Debug.Log("Amount: " + AmountOfDoors());
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (neighbors[i] != null)
            {
                Debug.Log(i);
                Debug.Log("Neighbor id: " + neighbors[i].name);
            }
                
        }  
        
    }

    public void EnterRoom() => map.SetActive(true);
    public void ExitRoom()  => map.SetActive(false);
}