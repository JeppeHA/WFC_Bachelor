using System;
using System.Collections.Generic;
using UnityEngine;

public class GraphOrganiser : MonoBehaviour
{
    public int maxAmountOfNodesForGraph;
    public MapGraph graphPrefab;
    public WFCGenerator generator;
    public List<MapGraph> graphs = new List<MapGraph>();
    public List<MapNode> locations = new List<MapNode>();
    
    private int graphCount = 0;

    private void Start()
    {
        MapGraph startMap = Instantiate(graphPrefab);
        startMap.SetMaxAmountOfNodes(maxAmountOfNodesForGraph);
        startMap.SetGenerator(generator);
        startMap.SetGraphIndex(graphCount);
        startMap.StartGeneration();
        
        graphs.Add(startMap);
        graphCount++;
        CreateNewGraph(2);
        ConnectGraphs(startMap, graphs[1]);
        startMap.nodes[startMap.nodes.Count - 1].PrintNeighbors();
        graphs[1].nodes[0].PrintNeighbors();
    }

    private void CreateNewGraph(int nodesCount)
    {
        MapGraph newGraph = Instantiate(graphPrefab);
        newGraph.SetPreviousConnectorNode(graphs[graphCount - 1].nodes[^1]);
        newGraph.SetGraphIndex(graphCount);
        newGraph.SetMaxAmountOfNodes(nodesCount);
        newGraph.SetGenerator(generator);
        newGraph.StartGeneration();
        newGraph.SetPreviousConnectorNode(newGraph.nodes[^1]);
        graphs.Add(newGraph);
        graphCount++;
    }

    private void ConnectGraphs(MapGraph src, MapGraph dst)
    {
        MapNode connectorNode = src.nodes[src.nodes.Count - 1];
        foreach (int dir in connectorNode.directions)
        {
            if(dir == -1) continue;
            if(connectorNode.neighbors[dir] != null) continue;
            connectorNode.neighbors[dir] = dst.nodes[0];
            
        }
        //src.nodes[src.nodes.Count - 1].neighbors
    }
}
