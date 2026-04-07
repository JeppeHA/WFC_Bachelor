    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
    using Random = UnityEngine.Random;

    public class MapGraph : MonoBehaviour
    {

        public List<MapNode> nodes = new List<MapNode>();
        //public List
        private int graphIndex = 0;
        private MapNode currentNode;
        private WFCGenerator generator;
        private Location location;

        private MapNode PreviousConnectorNode;
        
        private int maxAmountOfNodes;
        private int[] previousDirections;

        private int id;
        private int index = 0;

        public void StartGeneration()
        {
            Debug.Log("MapGraph Start");
            GenerateGraph();
            currentNode = nodes[0];
            currentNode.EnterRoom();
            //nodes[nodes.Count - 1].PrintNeighbors();
        }
        
        public void RequestTransition(MapNode from, int direction)
        {
            if (from != currentNode)
            {
                Debug.LogWarning("Transition triggered from a node that isn't current.");
                return;
            }
            int opposite = (direction + 2) % 4;
            
            if (from.neighbors[direction] == null)
            {
                Debug.LogWarning("No neighbor in that direction");
                return;
            }
            Debug.Log($"Direction {direction}");   
            Debug.Log($"Opposite {opposite}");
            
            SwitchRoom(currentNode.neighbors[direction]);
        }

        private void GenerateGraph()
        {
            Queue<MapNode> queue = new Queue<MapNode>();

            MapNode startNode = GenerateNode();
            if (graphIndex > 0)
            {
                startNode.isConnector = true;
                Debug.Log($"previous connector {PreviousConnectorNode}");
                foreach (int dir in PreviousConnectorNode.directions)
                {
                    if(dir == -1) continue;
                    if(PreviousConnectorNode.neighbors[dir] != null) continue;
                    int opposite = (dir + 2) % 4;
                    startNode.neighbors[opposite] = PreviousConnectorNode;
                }
            }
                
            nodes.Add(startNode);
            currentNode = startNode;
            queue.Enqueue(startNode);
            
            while (queue.Count > 0 && nodes.Count < maxAmountOfNodes)
            {
                MapNode node = queue.Dequeue();

                foreach (int dir in new int[] { 0, 1, 2, 3 })
                {

                    if (node.neighbors[dir] != null) continue;
                    if (nodes.Count >= maxAmountOfNodes) break;
                    if (Random.value > 0.5f) continue;

                    MapNode neighbor = GenerateNode();
                    node.neighbors[dir] = neighbor;

                    int opposite = (dir + 2) % 4;
                    neighbor.neighbors[opposite] = node;

                    nodes.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
            
            AssignGraphCoordinates(); 
            
            nodes[nodes.Count - 1].isConnector = true;
            foreach (var node in nodes)
                FinalizeNode(node);
        }
        
        private MapNode GenerateNode()
        {
            MapNode node = new MapNode();
            
            node.name = graphIndex + "." + id;
            id++;
            int remaining = maxAmountOfNodes - nodes.Count;

            // Always allow at least 1 (for parent connection)
            //int maxDoors = Mathf.Clamp(remaining, 1, 4);
            //int doorCount = Random.Range(1, maxDoors + 1);
            //int doorCount = Random.Range(1, 5);

            node.directions = new int[4] { -1, -1, -1, -1 };
            Debug.Log($"nodes count {nodes.Count}");
            
            return node;
        }

        private MapNode ConnectNode(MapNode dst)
        {
            MapNode node = new MapNode();
            
            node.name = id.ToString();
            id++;
            
            //node. = nodes[nodes.Count - 1];
            
            
            return node;
        }
        
        private void AssignGraphCoordinates()
        {
            // Direction index → (col offset, row offset)
            // Must match your GetEdgeIndex: 0=West(-X), 1=East(+X), 2=South(-Z), 3=North(+Z)
            Vector2Int[] dirToOffset = new Vector2Int[]
            {
                new Vector2Int(-1,  0), // 0 West
                new Vector2Int( 1,  0), // 1 East
                new Vector2Int( 0, -1), // 2 South
                new Vector2Int( 0,  1), // 3 North
            };

            Dictionary<MapNode, Vector2Int> coords = new Dictionary<MapNode, Vector2Int>();
            Queue<MapNode> queue = new Queue<MapNode>();

            coords[nodes[0]] = Vector2Int.zero;
            queue.Enqueue(nodes[0]);

            while (queue.Count > 0)
            {
                MapNode current = queue.Dequeue();
                Vector2Int currentCoord = coords[current];

                for (int dir = 0; dir < 4; dir++)
                {
                    MapNode neighbor = current.neighbors[dir];
                    if (neighbor == null || coords.ContainsKey(neighbor)) continue;

                    Vector2Int neighborCoord = currentCoord + dirToOffset[dir];
                    coords[neighbor] = neighborCoord;
                    queue.Enqueue(neighbor);
                }
            }

            // Write coords back and set map positions
            foreach (var kvp in coords)
            {
                kvp.Key.graphCoord = kvp.Value;
            }
        }


        private GameObject GenerateRoom(MapNode node, int doorCount, int[] directions) 
        {
            generator.modules = generator.moduleGenerator.GetModules().ToArray();
            generator.transitions = doorCount;
            Debug.Log($"Generating room for {doorCount} transitions");
            generator.Generate(directions);

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

                node.transitions[direction] = t; 
            }
        }
        
        private void FinalizeNode(MapNode node)
        {
            List<int> validDirs = new List<int>();

            for (int i = 0; i < 4; i++)
            {
                if (node.neighbors[i] != null)
                    validDirs.Add(i);
            }

            for (int i = 0; i < 4; i++)
            {
                if(!node.isConnector) break;
                if(node.neighbors[i] != null) continue;
                validDirs.Add(i);
                break;
            }

            int[] directions = new int[4] { -1, -1, -1, -1 };

            foreach (int dir in validDirs)
            {
                directions[dir] = dir;
            }
               

            node.directions = directions;
           // generator.currentMapCoord = node.graphCoord;
            node.map = GenerateRoom(node, validDirs.Count, directions);
           // node.ExitRoom();
        }
        
        

        private void SwitchRoom(MapNode next)
        {
            Debug.Log($"Switching to {next.name}");
            currentNode.ExitRoom();
            currentNode = next;
            currentNode.EnterRoom();
            
        }

        public void SetMaxAmountOfNodes(int amount)
        {
            maxAmountOfNodes = amount;
        }

        public void SetGenerator(WFCGenerator newGenerator)
        {
            generator = newGenerator;
        }

        public void SetGraphIndex(int graphIndex)
        {
            this.graphIndex = graphIndex;
        }

        public void SetPreviousConnectorNode(MapNode previousNode)
        {
            PreviousConnectorNode = previousNode;
        }
        
        
    }