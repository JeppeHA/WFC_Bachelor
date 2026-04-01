using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGraph : MonoBehaviour
{
    public List<MapNode> nodes;
    private MapNode CurrentNode;
    public WFCGenerator generator;

    void Start()
    {
        nodes = new List<MapNode>();
        GenerateNode();
        CurrentNode = nodes[0];
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            //SwitchRooms();
        }
    }
    
    private GameObject GenerateRoom(int neighbours)
    {
        generator.modules = generator.moduleGenerator.GetModules().ToArray();
        generator.transitions = neighbours;
        generator.Generate();
        return generator.GetMap();
    }

    private void GenerateNode()
    {
        MapNode node = new MapNode();
        node.SetNeighbors(new MapNode[Random.Range(1, 5)]);
        node.map = GenerateRoom(node.neighbors.Length);
        node.SetSpawnPositions(generator.GetTransitionPositions());
        nodes.Add(node);
    }

    private MapNode[] GenerateNeighbors(MapNode node)
    {
        return null;
    }

    
    private void SwitchRooms(MapNode newRoom)
    {
        //if(newRoom.map == null)
            //newRoom.map = GenerateRoom();
        
        //CurrentNode.SetNeighbors();
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
