using System.Collections.Generic;
using UnityEngine;

//[System.Serializable]
public class MapNode
{
    public MapNode[] neighbors = new MapNode[4]; // fixed size 4, null = no door or unvisited
    public bool[] hasDoor = new bool[4];         // which slots actually have a door
    public List<GameObject> spawnPositions;
    public GameObject map;
    
    public string name;

    public int AmountOfDoors()
    {
        int doors = 0;
        for (int i = 0; i < hasDoor.Length; i++)
        {
            if (hasDoor[i])
                doors++;
        }
        return doors;
    }

    public void PrintNeighbors()
    {
        Debug.Log("-----------------------------");
        Debug.Log("Node: " + name);
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