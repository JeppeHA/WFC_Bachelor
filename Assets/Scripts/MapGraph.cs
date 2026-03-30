using System.Collections.Generic;
using UnityEngine;

public class MapGraph : MonoBehaviour
{
    public List<MapNode> nodes;
    public WFCGenerator generator;

    void Start()
    {
        nodes = new List<MapNode>();
        GenerateFirstNode();
        GenerateRoom(nodes[0]);
        PrintNodes();
    }
    
    private void GenerateRoom(MapNode node)
    {
        generator.Generate();
        node.map = generator.GetMap();
    }

    private void GenerateFirstNode()
    {
        MapNode firstRoom = new MapNode();
        firstRoom.SetNeighbors(new MapNode[Random.Range(1, 5)]); 
        nodes.Add(firstRoom);
    }

    private void PrintNodes()
    {
        Debug.Log("Node count: " + nodes.Count);
        foreach (MapNode node in nodes)
        {
            Debug.Log(node.map.name);
        }
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
