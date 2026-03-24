using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Wave Function Collapse generator for a 3D grid.
/// 
/// SETUP:
/// 1. Create WFCTile assets (Assets > WFC > Tile) and assign prefabs + neighbor rules.
/// 2. Add this component to a GameObject in your scene.
/// 3. Assign tiles array in the Inspector.
/// 4. Press Play (or call Generate() at runtime).
/// 
/// NEIGHBOR RULE FORMAT:
/// Each tile has 6 arrays (posX, negX, posY, negY, posZ, negZ).
/// Fill each array with the tile indices that are ALLOWED to sit next to
/// this tile in that direction.
/// </summary>
public class WFCGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridX = 8;
    public int gridY = 4;
    public int gridZ = 8;
    public float cellSize = 1f;

    [Header("Tiles")]
    public WFCTile[] tiles;
    public ModuleLoader moduleLoader;
    public MeshCombiner meshCombiner;

    [Header("Generation")]
    public bool generateOnStart = true;
    public bool stepByStep = false;         // Slow-motion debug mode
    public float stepDelay = 0.05f;
    public int maxRetries = 5;






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
        moduleLoader.LoadModules();
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

            bool success = false;

            while (true)
            {
                Vector3Int? cell = PickLowestEntropyCell();

                if (cell == null) { success = true; break; } 

                CollapseCell(cell.Value);
                bool ok = Propagate(cell.Value);

                if (!ok)
                {
                    Debug.LogWarning($"WFC contradiction on attempt {attempt + 1}. Retrying…");
                    break;
                }

                if (stepByStep) yield return new WaitForSeconds(stepDelay);
            }

            if (success)
            {
                //StartCoroutine(SpawnTilesE());
                SpawnTiles();
                Debug.Log($"WFC finished successfully on attempt {attempt + 1}.");


                /*foreach (var obj in spawnedObjects)
                {
                   MeshFilter mesh = obj.GetComponent<MeshFilter>();
                    if (mesh == null)
                        mesh = obj.transform.GetChild(0).GetComponent<MeshFilter>();
                    meshCombiner.AddMeshes(mesh);

                }

                meshCombiner.Combine();*/

                yield break;
            }
        }


        

        Debug.LogError("WFC failed after all retries. Check your neighbor rules.");
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

        // Weighted random selection
        float totalWeight = cell.possibleTiles.Sum(i => tiles[i].weight);
        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        int chosen = cell.possibleTiles[0];

        foreach (int i in cell.possibleTiles)
        {
            cumulative += tiles[i].weight;
            if (roll <= cumulative) { chosen = i; break; }
        }

        cell.collapsed = true;
        cell.collapsedTileIndex = chosen;
        cell.possibleTiles = new List<int> { chosen };
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
            if (tile.prefab == null) continue;

            Vector3 worldPos = transform.position + new Vector3(x, y, z) * cellSize;
            GameObject go = Instantiate(tile.prefab, worldPos, Quaternion.identity, transform);
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
                    if (tile.prefab == null) continue;

                    Vector3 worldPos = transform.position + new Vector3(x, y, z) * cellSize;
                    GameObject go = Instantiate(tile.prefab, worldPos, Quaternion.identity, transform);
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
