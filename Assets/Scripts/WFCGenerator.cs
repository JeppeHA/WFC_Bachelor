using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class WFCGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridX = 8;
    public int gridY = 4;
    public int gridZ = 8;
    public float cellSize = 1f;

    [Header("Modules")]
    public WFCModule[] modules;
    public MeshCombiner meshCombiner;

    [Header("Generation")]
    public bool generateOnStart = true;
    public ModuleGenerator moduleGenerator;
    public bool stepByStep = false;         // Slow-motion debug mode
    public float stepDelay = 0.05f;
    public int maxRetries = 5;
    public GameObject mapPrefab;
    private GameObject mapParent;
    
    public int multiplier;

    [Header("PreSpawns")] 
    public int transitions;
    [SerializeField]
    private List<Vector3> TransitionPositions = new List<Vector3>();
    private int[] directions = new int[4];
    public MapGraph graph;

    private int mapNumber = 1;

    
    private Dictionary<int, GameObject> transitionObjectsByDirection = new Dictionary<int, GameObject>();
    private Dictionary<Vector3Int, int> transitionGridPositions = new Dictionary<Vector3Int, int>();
    

    // ── Internal state ────────────────────────────────────────────────────────
    private WFCCell[,,] grid;
    private List<GameObject> spawnedObjects = new List<GameObject>();

    // ── Direction helpers ─────────────────────────────────────────────────────
    private static readonly Vector3Int[] Directions = {
        Vector3Int.right,   // 0 → posX
        Vector3Int.left,    // 1 → negX
        Vector3Int.up,      // 2 → posY
        Vector3Int.down,    // 3 → negY
        Vector3Int.forward, // 4 → posZ
        Vector3Int.back     // 5 → negZ
    };

    // Returns the allowed-neighbor array for a module in a given direction index.
    private int[] GetNeighbors(WFCModule module, int dirIndex) => dirIndex switch
    {
        0 => module.posXNeighbors,
        1 => module.negXNeighbors,
        2 => module.posYNeighbors,
        3 => module.negYNeighbors,
        4 => module.posZNeighbors,
        5 => module.negZNeighbors,
        _ => new int[0]
    };

    // ── Unity lifecycle ────────────────────────────────────────-───────────────
    private void Start()
    {
        
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        StopAllCoroutines();
        StartCoroutine(GenerateCoroutine());
       
    }

    // ── Core algorithm ────────────────────────────────────────────────────────
    private IEnumerator GenerateCoroutine()
{
    //ClearSpawned();
    
    spawnedObjects.Clear();

    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        InitialiseGrid();

        // Pre-collapse transitions and collect their positions
        List<Vector3Int> preCollapsedPositions = new List<Vector3Int>();
        
        if (!CollapseTransitions(preCollapsedPositions))
        {
            Debug.LogWarning($"Pre-collapse failed on attempt {attempt + 1}. Retrying…");
            continue;
        }

        // Propagate all pinned cells together so their constraints race outward jointly
        if (!PropagateAll(preCollapsedPositions))
        {
            Debug.LogWarning($"Pre-collapse propagation failed on attempt {attempt + 1}. Retrying…");
            continue;
        }

        bool success = false;

        while (true)
        {
            Vector3Int? cell = PickLowestEntropyCell();
            if (cell == null) { success = true; break; }

            CollapseCell(cell.Value);

            if (!Propagate(cell.Value))
            {
                Debug.LogWarning($"WFC contradiction on attempt {attempt + 1}. Retrying…");
                meshCombiner.Combine();
                break;
            }

            if (stepByStep) yield return new WaitForSeconds(stepDelay);
        }

        if (success)
        {
            SpawnModules();
            Debug.Log($"WFC finished successfully on attempt {attempt + 1}.");
            meshCombiner.Combine();
            yield break;
        }
    }

    Debug.LogWarning("WFC failed after all retries. Check your neighbor rules.");
    StopAllCoroutines();
    StartCoroutine(GenerateCoroutine());
}





    // ── Step 0: Initialise ────────────────────────────────────────────────────
    private void InitialiseGrid()
    {
        transitionGridPositions.Clear();
        grid = new WFCCell[gridX, gridY, gridZ];
        for (int x = 0; x < gridX; x++)
        for (int y = 0; y < gridY; y++)
        for (int z = 0; z < gridZ; z++)
            grid[x, y, z] = new WFCCell(modules.Length);
    }

    // ── Step 1: Observe (lowest entropy) ─────────────────────────────────────
    private Vector3Int? PickLowestEntropyCell()
    {
        float minEntropy = float.MaxValue;
        Vector3Int? best = null;

        for (int x = 0; x < gridX; x++)
        for (int y = 0; y < gridY; y++)
        for (int z = 0; z < gridZ; z++)
        {
            WFCCell cell = grid[x, y, z];
            if (cell.collapsed) continue;

            float e = cell.Entropy(modules) + Random.value * 0.01f; // tiny noise breaks ties
            if (e < minEntropy) { minEntropy = e; best = new Vector3Int(x, y, z); }
        }

        return best;
    }

    // ── Step 2: Collapse ──────────────────────────────────────────────────────
    private void CollapseCell(Vector3Int pos)
    {
        WFCCell cell = grid[pos.x, pos.y, pos.z];

        int baseTransitionIndex = modules.Length - moduleGenerator.numberOfLayers;

        // Only consider non-transition modules for normal collapse
        List<int> candidates = cell.possibleModules
            .Where(i => i < baseTransitionIndex)
            .ToList();

        // Fall back to all possible modules if filtering left nothing
        if (candidates.Count == 0)
            candidates = cell.possibleModules;

        float totalWeight = candidates.Sum(i => modules[i].weight);
        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        int chosen = candidates[0];

        foreach (int i in candidates)
        {
            cumulative += modules[i].weight;
            if (roll <= cumulative) { chosen = i; break; }
        }

        cell.collapsed = true;
        cell.collapsedModuleIndex = chosen;
        cell.possibleModules = new List<int> { chosen };
    }
    
    private bool CollapseTransitions(List<Vector3Int> preCollapsedPositions)
    {
        directions = new int[4];
        int baseTransitionIndex = modules.Length - moduleGenerator.numberOfLayers;
        int[] prevEdges = new int[transitions];
        for (int i = 0; i < transitions; i++)
        {
            prevEdges[i] = -1;
        }
        List<int> edgePool = new List<int> { 0, 1, 2, 3};

        for (int i = 0; i < transitions; i++)
        {
            int moduleIndex = baseTransitionIndex + Random.Range(0, moduleGenerator.numberOfLayers);

            if (moduleIndex < 0 || moduleIndex >= modules.Length)
            {
                Debug.LogError($"CollapseTransitions: module index {moduleIndex} out of range.");
                return false;
            }

            int edgeType;

            // Pick a unique edgeType
            do
            {
                edgeType = edgePool[Random.Range(0, edgePool.Count)];
            }
            while (System.Array.Exists(prevEdges, e => e == edgeType));
            
            prevEdges[i] = edgeType;
            

            //Debug.Log(edgeType);

            var edge = GetEdgeIndex(gridX, gridZ, edgeType);
            int x = edge.row;
            int z = edge.col;

            if (x < 0 || x >= gridX || z < 0 || z >= gridZ)
            {
                Debug.LogError($"CollapseTransitions: edge pos ({x},0,{z}) out of grid.");
                return false;
            }

            WFCCell cell = grid[x, 0, z];
            cell.collapsed = true;
            cell.collapsedModuleIndex = moduleIndex;
            cell.possibleModules = new List<int> { moduleIndex };
            
            preCollapsedPositions.Add(new Vector3Int(x, 0, z));
            transitionGridPositions[new Vector3Int(x, 0, z)] = edgeType; 
            
        }
        Array.Reverse(prevEdges);
        directions = prevEdges;
        return true;
    }
    
    public (int row, int col) GetEdgeIndex(int rows, int cols, int edgeType)
    {
        if (rows <= 0 || cols <= 0)
            throw new ArgumentException("Matrix dimensions must be positive.");
        switch (edgeType)
        {
            case 0: return (0, Random.Range(0, cols));              // -X West
            case 1: return (rows - 1, Random.Range(0, cols));       // +X East
            case 2: return (Random.Range(0, rows), 0);              // -Z South
            case 3: return (Random.Range(0, rows), cols - 1);       // +Z North
            default: throw new Exception("Unexpected edge type");
        }
    }
    
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
   //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    
    // ── Step 3: Propagate  ───────────────────────────────────
    private bool Propagate(Vector3Int start)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            WFCCell currentCell = grid[current.x, current.y, current.z];

            for (int d = 0; d < 6; d++)
            {
                if (d == 2 || d == 3) continue;
                Vector3Int neighborPos = current + Directions[d];

                // Bounds check
                if (!InBounds(neighborPos)) continue;

                WFCCell neighbor = grid[neighborPos.x, neighborPos.y, neighborPos.z];
                if (neighbor.collapsed) continue;

                // Collect all module IDs that currentCell allows in direction d
                HashSet<int> allowed = new HashSet<int>();
                foreach (int moduleIdx in currentCell.possibleModules)
                    foreach (int allowedNeighbor in GetNeighbors(modules[moduleIdx], d))
                        allowed.Add(allowedNeighbor);

                // Remove from neighbor any module NOT in allowed set
                int before = neighbor.possibleModules.Count;
                neighbor.possibleModules.RemoveAll(t => !allowed.Contains(t));

                if (neighbor.IsContradiction) return false;

                // If we removed options, the neighbor must re-propagate
                if (neighbor.possibleModules.Count < before)
                    queue.Enqueue(neighborPos);
            }
        }

        return true;
    }
    
    
    // Seeds queue with every pinned cell so all constraints spread simultaneously
    private bool PropagateAll(List<Vector3Int> starts)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();

        foreach (var pos in starts)
            queue.Enqueue(pos);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            WFCCell currentCell = grid[current.x, current.y, current.z];

            for (int d = 0; d < 6; d++)
            {
                if (d == 2 || d == 3) continue; // skip Y

                Vector3Int neighborPos = current + Directions[d];
                if (!InBounds(neighborPos)) continue;

                WFCCell neighbor = grid[neighborPos.x, neighborPos.y, neighborPos.z];
                if (neighbor.collapsed) continue;

                HashSet<int> allowed = new HashSet<int>();
                foreach (int moduleIdx in currentCell.possibleModules)
                foreach (int allowedNeighbor in GetNeighbors(modules[moduleIdx], d))
                    allowed.Add(allowedNeighbor);

                int before = neighbor.possibleModules.Count;
                neighbor.possibleModules.RemoveAll(t => !allowed.Contains(t));

                if (neighbor.IsContradiction) return false;

                if (neighbor.possibleModules.Count < before)
                    queue.Enqueue(neighborPos);
            }
        }

        return true;
    }

    // ── Step 4: Spawn ─────────────────────────────────────────────────────────
    private void SpawnModules()
    {
        transitionObjectsByDirection.Clear(); // reset from previous generation
        
        mapParent = Instantiate(mapPrefab,new Vector3(mapNumber * multiplier,0,0), Quaternion.identity);
        
        for (int x = 0; x < gridX; x++)
        for (int y = 0; y < gridY; y++)
        for (int z = 0; z < gridZ; z++) 
        {
            WFCCell cell = grid[x, y, z];
            if (!cell.collapsed || cell.collapsedModuleIndex < 0) continue;

            WFCModule module = modules[cell.collapsedModuleIndex];
            if (module.obj == null) continue;

            Vector3 worldPos;
            //if (module.name.ToLower().Contains("stair"))
                //worldPos = transform.position + new Vector3(x, module.layer + 1, z) * cellSize;
            worldPos = mapParent.transform.position + new Vector3(x, module.layer, z) * cellSize;

            GameObject go = Instantiate(module.obj, worldPos, Quaternion.identity, mapParent.transform);
            spawnedObjects.Add(go);
            //meshCombiner.AddMeshes(go.transform.GetChild(0).GetComponent<MeshFilter>());
            
            Vector3Int gridPos = new Vector3Int(x, y, z);
            if (transitionGridPositions.TryGetValue(new Vector3Int(x, 0, z), out int direction))
            {
                transitionObjectsByDirection[direction] = go;
            }
            
        }
        mapNumber++;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private bool InBounds(Vector3Int p) =>
        p.x >= 0 && p.x < gridX &&
        p.y >= 0 && p.y < gridY &&
        p.z >= 0 && p.z < gridZ;

    [ContextMenu("Clear")]
    public void ClearSpawned()
    {
        foreach (var go in spawnedObjects)
            if (go != null) DestroyImmediate(go);
        spawnedObjects.Clear();
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Vector3 gridCenter = transform.position + new Vector3(gridX, gridY, gridZ) * cellSize * 0.5f;
        Gizmos.DrawWireCube(gridCenter - Vector3.one * cellSize * 0.5f,
                            new Vector3(gridX, gridY, gridZ) * cellSize);
    }

    public GameObject GetMap()
    {
        return mapParent;
    }
    
    public Dictionary<int, GameObject> GetTransitionObjects()
    {
        return transitionObjectsByDirection;
    }

    public int[] GetDirections()
    {
        return directions;
    }
    
}
