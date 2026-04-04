using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MapGraph : MonoBehaviour
{

    public List<MapNode> nodes = new List<MapNode>();
    private MapNode currentNode;
    public WFCGenerator generator;
    
    [SerializeField]
    private int maxAmountOfNodes;
    private int[] previousDirections;
    private int[] currentDirections;
    private int id;
    private int index = 0;
    private void Start()
    {
        Debug.Log("MapGraph Start");
        GenerateGraph(0);
        currentNode = nodes[0];
        currentNode.PrintNeighbors();
        nodes[1].PrintNeighbors();
        nodes[2].PrintNeighbors();
    }
    public void RequestTransition(MapNode from, int direction)
    {
        if (from != currentNode)
        {
            Debug.LogWarning("Transition triggered from a node that isn't current.");
            return;
        }
        int opposite = (direction + 2) % currentNode.AmountOfDoors();
        
        SwitchRoom(currentNode.neighbors[opposite]);
    }

    private int GenerateGraph(int n)
    {
        if (n >= maxAmountOfNodes)
        {
            return(0);
        }
           
        
        HashSet<int> taken = new HashSet<int>();
        MapNode _node = GenerateNode();
        currentNode = _node;
        int num = 1;
        index = 0;
       while(taken.Count < _node.AmountOfDoors())
       {
           if(num <= 0)
               break;
            int index = Random.Range(0, _node.neighbors.Length - num);
            if (!taken.Contains(index))
            {
                taken.Add(index);
                _node.neighbors[index] = GenerateNode();
                num++;
            }
        }
       previousDirections = currentDirections;
        
        return (n + _node.AmountOfDoors() + 1); 
    }
    
    private MapNode GenerateNode()
    {
        MapNode node = new MapNode();
        
        node.name = id.ToString();
        id++;
        int doorCount = 2;//Random.Range(2, 5);
        for (int i = 0; i < doorCount; i++)
        {
            node.hasDoor[i] = true;
        }
        node.map = GenerateRoom(node, doorCount);
        
        if (nodes.Count >= 1)
        {
            currentDirections = generator.GetDirections();
            Debug.Log("SetSelfAsNeighbors"); 
            SetSelfAsNeighbor(node, currentNode, index);
        }
        else
        {
            previousDirections = generator.GetDirections();
        }
        nodes.Add(node);
        index++;
        return node;
    }


    private void SetSelfAsNeighbor(MapNode src, MapNode dst, int index)
    {
        Debug.Log("Src: " + src.name + " dst: " + dst.name);
        
        src.neighbors[currentDirections[index] % 4] = dst;
        
       /* for (int i = 0; i < src.neighbors.Length; i++)
        {
            if (src.neighbors[i] != null)
            {
                continue;
            }
            src.neighbors[i] = dst;
            break;
        }*/

       
    }
    

    private GameObject GenerateRoom(MapNode node, int doorCount) 
    {
        generator.modules = generator.moduleGenerator.GetModules().ToArray();
        generator.transitions = doorCount;
        Debug.Log($"Generating room for {doorCount} transitions");
        generator.Generate();

        StampTransitions(node, generator.GetTransitionObjects()); 
        return generator.GetMap();
    }

    // Wire up each spawned door GameObject to know its owner node and direction
    private void StampTransitions(MapNode node, Dictionary<int, GameObject> transitionObjects)
    {
        foreach (var kvp in transitionObjects)
        {
            int direction = kvp.Key;
            GameObject doorObj = kvp.Value;

            Transition t = doorObj.GetComponent<Transition>();
            if (t == null) t = doorObj.AddComponent<Transition>();

            t.ownerNode = node;
            t.direction = direction;
        }
    }

    private void SwitchRoom(MapNode next)
    {
        Debug.Log($"Switching to {next.name}");
        currentNode.ExitRoom();
        currentNode = next;
        currentNode.EnterRoom();
        
    }
    
    
}