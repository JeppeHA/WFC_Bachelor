using System.Collections.Generic;
using UnityEngine;

public class MapGraph : MonoBehaviour
{
    public List<MapNode> nodes;
    public WFCGenerator generator;

    void Start()
    {
        nodes = new List<MapNode>();
        GenerateNode();
        Debug.Log(nodes.Count);
        Debug.Log(nodes[0].map.name);
    }
    
    private GameObject GenerateRoom()
    {
        generator.modules = generator.moduleGenerator.GetModules().ToArray();
        generator.Generate();
        return generator.GetMap();
    }

    private void GenerateNode()
    {
        MapNode node = new MapNode();
        node.SetNeighbors(new MapNode[Random.Range(1, 5)]);
        node.map = GenerateRoom();
        nodes.Add(node);
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
