using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class WFCGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridX = 8;
    public int gridY = 4;
    public int gridZ = 8;
    public float cellSize = 1f;

    [Header("Tiles")]
    public WFCTile[] tiles;
    public MeshCombiner meshCombiner;

    [Header("Generation")]
    public bool generateOnStart = true;
    public ModuleGenerator moduleGenerator;
    public bool stepByStep = false;         // Slow-motion debug mode
    public float stepDelay = 0.05f;
    public int maxRetries = 5;

    [Header("PreSpawns")] 
    public int transitions;
    public WFCTile[] transitionModules;
    

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

    // Returns the allowed-neighbor array for a tile in a given direction index.
    private int[] GetNeighbors(WFCTile tile, int dirIndex) => dirIndex switch
    {
        0 => tile.posXNeighbors,
        1 => tile.negXNeighbors,
        2 => tile.posYNeighbors,
        3 => tile.negYNeighbors,
        4 => tile.posZNeighbors,
        5 => tile.negZNeighbors,
        _ => new int[0]
    };

    // Opposite direction index (needed for constraint propagation)
    private int Opposite(int dir) => dir ^ 1;

    // ── Unity lifecycle ────────────────────────────────────────-───────────────
    private void Start()
    {
        tiles = moduleGenerator.GetModules().ToArray();
   
        if (generateOnStart) StartCoroutine(GenerateCoroutine());
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
    ClearSpawned();

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
                break;
            }

            if (stepByStep) yield return new WaitForSeconds(stepDelay);
        }

        if (success)
        {
            SpawnTiles();
            Debug.Log($"WFC finished successfully on attempt {attempt + 1}.");
            yield break;
        }
    }

    Debug.LogError("WFC failed after all retries. Check your neighbor rules.");
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
            foreach (int tileIdx in currentCell.possibleTiles)
                foreach (int allowedNeighbor in GetNeighbors(tiles[tileIdx], d))
                    allowed.Add(allowedNeighbor);

            int before = neighbor.possibleTiles.Count;
            neighbor.possibleTiles.RemoveAll(t => !allowed.Contains(t));

            if (neighbor.IsContradiction) return false;

            if (neighbor.possibleTiles.Count < before)
                queue.Enqueue(neighborPos);
        }
    }

    return true;
}

    // ── Step 0: Initialise ────────────────────────────────────────────────────
    private void InitialiseGrid()
    {
        grid = new WFCCell[gridX, gridY, gridZ];
        for (int x = 0; x < gridX; x++)
        for (int y = 0; y < gridY; y++)
        for (int z = 0; z < gridZ; z++)
            grid[x, y, z] = new WFCCell(tiles.Length);
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

            float e = cell.Entropy(tiles) + Random.value * 0.01f; // tiny noise breaks ties
            if (e < minEntropy) { minEntropy = e; best = new Vector3Int(x, y, z); }
        }

        return best;
    }

    // ── Step 2: Collapse ──────────────────────────────────────────────────────
    private void CollapseCell(Vector3Int pos)
    {
        WFCCell cell = grid[pos.x, pos.y, pos.z];

        int baseTransitionIndex = tiles.Length - moduleGenerator.numberOfLayers;

        // Only consider non-transition tiles for normal collapse
        List<int> candidates = cell.possibleTiles
            .Where(i => i < baseTransitionIndex)
            .ToList();

        // Fall back to all possible tiles if filtering left nothing
        // (shouldn't happen if propagation is working, but safe to handle)
        if (candidates.Count == 0)
            candidates = cell.possibleTiles;

        float totalWeight = candidates.Sum(i => tiles[i].weight);
        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        int chosen = candidates[0];

        foreach (int i in candidates)
        {
            cumulative += tiles[i].weight;
            if (roll <= cumulative) { chosen = i; break; }
        }

        cell.collapsed = true;
        cell.collapsedTileIndex = chosen;
        cell.possibleTiles = new List<int> { chosen };
    }
    
    private bool CollapseTransitions(List<Vector3Int> preCollapsedPositions)
    {
        int baseTransitionIndex = tiles.Length - moduleGenerator.numberOfLayers;
        int[] prevEdges = new int[transitions];
        List<int> edgePool = new List<int> { 0, 1, 2, 3 };
        ShuffleList(edgePool);

        for (int i = 0; i < transitions; i++)
        {
            int tileIndex = baseTransitionIndex + Random.Range(0, moduleGenerator.numberOfLayers);

            if (tileIndex < 0 || tileIndex >= tiles.Length)
            {
                Debug.LogError($"CollapseTransitions: tile index {tileIndex} out of range.");
                return false;
            }

            int edgeType;

            // Pick a unique edgeType
            do
            {
                edgeType = edgePool[Random.Range(0, edgePool.Count)];
            }
            while (System.Array.Exists(prevEdges, e => e == edgeType));

            // Store it so it can't be reused
            prevEdges[i] = edgeType;

            Debug.Log(edgeType);

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
            cell.collapsedTileIndex = tileIndex;
            cell.possibleTiles = new List<int> { tileIndex };

            preCollapsedPositions.Add(new Vector3Int(x, 0, z));
        }

        return true;
    }
    
// Deterministic version — takes edgeType as a parameter
    public (int row, int col) GetEdgeIndex(int rows, int cols, int edgeType)
    {
        if (rows <= 0 || cols <= 0)
            throw new ArgumentException("Matrix dimensions must be positive.");

        switch (edgeType)
        {
            case 0: return (0, Random.Range(0, cols));              // Top row
            case 1: return (rows - 1, Random.Range(0, cols));       // Bottom row
            case 2: return (Random.Range(0, rows), 0);              // Left column
            case 3: return (Random.Range(0, rows), cols - 1);       // Right column
            default: throw new Exception("Unexpected edge type");
        }
    }

// Keep your original if needed elsewhere — renamed for clarity
    public (int row, int col) GetRandomEdgeIndex(int rows, int cols)
    {
        return GetEdgeIndex(rows, cols, Random.Range(0, 4));
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
   
    
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

                // Collect all tile IDs that currentCell allows in direction d
                HashSet<int> allowed = new HashSet<int>();
                foreach (int tileIdx in currentCell.possibleTiles)
                    foreach (int allowedNeighbor in GetNeighbors(tiles[tileIdx], d))
                        allowed.Add(allowedNeighbor);

                // Remove from neighbor any tile NOT in allowed set
                int before = neighbor.possibleTiles.Count;
                neighbor.possibleTiles.RemoveAll(t => !allowed.Contains(t));

                if (neighbor.IsContradiction) return false;

                // If we removed options, the neighbor must re-propagate
                if (neighbor.possibleTiles.Count < before)
                    queue.Enqueue(neighborPos);
            }
        }

        return true;
    }

    // ── Step 4: Spawn ─────────────────────────────────────────────────────────
    private void SpawnTiles()
    {
        for (int x = 0; x < gridX; x++)
        for (int y = 0; y < gridY; y++)
        for (int z = 0; z < gridZ; z++)
        {
            WFCCell cell = grid[x, y, z];
            if (!cell.collapsed || cell.collapsedTileIndex < 0) continue;

            WFCTile tile = tiles[cell.collapsedTileIndex];
            if (tile.obj == null) continue;

            Vector3 worldPos;
            if (tile.name.ToLower().Contains("stair"))
            {
              worldPos = transform.position + new Vector3(x, tile.layer + 1, z) * cellSize;
            }
            worldPos = transform.position + new Vector3(x, tile.layer, z) * cellSize;
            
            GameObject go = Instantiate(tile.obj, worldPos, Quaternion.identity, transform);
            spawnedObjects.Add(go);
        }
    }

    private IEnumerator SpawnTilesE()
    {
        for (int x = 0; x < gridX; x++)
            for (int y = 0; y < gridY; y++)
                for (int z = 0; z < gridZ; z++)
                {
                    WFCCell cell = grid[x, y, z];
                    if (!cell.collapsed || cell.collapsedTileIndex < 0) continue;

                    WFCTile tile = tiles[cell.collapsedTileIndex];
                    if (tile.obj == null) continue;

                    Vector3 worldPos = transform.position + new Vector3(x, y, z) * cellSize;
                    GameObject go = Instantiate(tile.obj, worldPos, Quaternion.identity, transform);
                    spawnedObjects.Add(go);
                    yield return new WaitForSeconds(0.1f);
                }
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
}
