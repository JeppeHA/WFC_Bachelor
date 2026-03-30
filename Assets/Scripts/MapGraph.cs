using System.Collections.Generic;
using UnityEngine;

public class MapGraph : MonoBehaviour
{
    public List<MapNode> nodes;
    public WFCGenerator generator;

    void Start()
    {
        nodes = new List<MapNode>();
        GenerateFirstRoom();
    }
    
    private void GenerateRoom()
    {
        
    }

    private void GenerateFirstRoom()
    {
        MapNode firstRoom = new MapNode();
        firstRoom.SetNeighbors(new MapNode[Random.Range(1, 5)]); 
        nodes.Add(firstRoom);
    }

    private void GenerateMap()
    {
        
    }

    private void AssesPlayer()
    {
        
    }

    private int CalculateAmountOfNeighbours()
    {
        return 1;
    }

    private void AddNeighbors()
    {
        
    }
    
    
}
