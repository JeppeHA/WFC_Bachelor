using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class WFC : MonoBehaviour
{
    private System.Random rnd = new System.Random();
    public int height;
    public int width;
    public int length;

    private Cell[,,] grid;

    public List<ModulData> Modules;

    public string[] socketIds;

    public ModuleLoader moduleLoader;


    private Dictionary<string, List<string>> compatibility;

    private bool contradictionFound = false;

    void Start()
    {
        Modules = moduleLoader.LoadModules();
        Debug.Log($"Loaded {Modules.Count} modules");

        InitializeGrid();
        RunWFC();
        if (!contradictionFound)
            Build();
        else
            Debug.LogError("WFC failed to resolve - not building");
    }
    private void InitializeGrid()
    {
        grid = new Cell[width, height, length];

        Queue<Vector3Int> queue = new Queue<Vector3Int>();

        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                for (int k = 0; k < length; k++)
                {
                        grid[i, j, k] = new Cell(Modules);
                    queue.Enqueue(new Vector3Int(i, j, k));
                }

        PropagateQueue(queue); 
    }

    private void ApplyBorderConstraints()
    {
        Queue<Vector3Int> changed = new Queue<Vector3Int>();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < length; z++)
                {
                    Cell cell = grid[x, y, z];
                    int before = cell.possibleModules.Count;

                    cell.possibleModules.RemoveAll(m => {
                        if (x == width - 1 && !m.GetValidNeighbours("pX").Contains("p-1")) return true;
                        if (x == 0 && !m.GetValidNeighbours("nX").Contains("p-1")) return true;
                        if (z == length - 1 && !m.GetValidNeighbours("pZ").Contains("p-1")) return true;
                        if (z == 0 && !m.GetValidNeighbours("nZ").Contains("p-1")) return true;

                        if (y == 0 && !m.GetValidNeighbours("nY").Contains("p-1")) return true;
                        if (y == 0 && m.constrain_from == "bot") return true;

                        if (y > 0 && m.constrain_to == "bot") return true;

                        if (y == height - 1 && !m.GetValidNeighbours("pY").Contains("p-1")) return true;

                        return false;
                    });

                    if (cell.possibleModules.Count != before)
                        changed.Enqueue(new Vector3Int(x, y, z));
                }

        PropagateQueue(changed);
    }

    private void PropagateQueue(Queue<Vector3Int> queue)
    {
        while (queue.Count > 0)
        {
            Vector3Int pos = queue.Dequeue();
            int cx = pos.x, cy = pos.y, cz = pos.z;

            CheckNeighbor(cx, cy, cz, cx + 1, cy, cz, "pX", "nX", queue);
            CheckNeighbor(cx, cy, cz, cx - 1, cy, cz, "nX", "pX", queue);
            CheckNeighbor(cx, cy, cz, cx, cy + 1, cz, "pY", "nY", queue);
            CheckNeighbor(cx, cy, cz, cx, cy - 1, cz, "nY", "pY", queue);
            CheckNeighbor(cx, cy, cz, cx, cy, cz + 1, "pZ", "nZ", queue);
            CheckNeighbor(cx, cy, cz, cx, cy, cz - 1, "nZ", "pZ", queue);
        }
    }

    private void Propagate(int x, int y, int z)
    {
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(new Vector3Int(x, y, z));
        PropagateQueue(queue);
    }

    /* private void Propagate(int x, int y, int z)
     {
         Queue<Vector3Int> queue = new Queue<Vector3Int>();
         queue.Enqueue(new Vector3Int(x,y,z));

         while (queue.Count > 0)
         {
             Vector3Int pos = queue.Dequeue();

             int cx = pos.x;
             int cy = pos.y;
             int cz = pos.z;

             // Check all 6 directions
             CheckNeighbor(cx, cy, cz, cx + 1, cy, cz, "pX", "nX", queue);
             CheckNeighbor(cx, cy, cz, cx - 1, cy, cz, "nX", "pX", queue);

             CheckNeighbor(cx, cy, cz, cx, cy + 1, cz, "pY", "nY", queue);
             CheckNeighbor(cx, cy, cz, cx, cy - 1, cz, "nY", "pY", queue);

             CheckNeighbor(cx, cy, cz, cx, cy, cz + 1, "pZ", "nZ", queue);
             CheckNeighbor(cx, cy, cz, cx, cy, cz - 1, "nZ", "pZ", queue);
         }
     }*/

    private void CheckNeighbor(int cx, int cy, int cz, int nx, int ny, int nz,
    string currentSide, string neighborSide, Queue<Vector3Int> queue)
    {
        if (nx < 0 || nx >= width || ny < 0 || ny >= height || nz < 0 || nz >= length)
            return;

        Cell current = grid[cx, cy, cz];
        Cell neighbor = grid[nx, ny, nz];

        bool changed = FilterNeighbor(current, neighbor, currentSide, neighborSide, nx, ny, nz); 

        if (neighbor.possibleModules.Count == 0)
        {
            contradictionFound = true;
            return;
        }

        if (changed)
            queue.Enqueue(new Vector3Int(nx, ny, nz));
    }

    private bool FilterNeighbor(Cell current, Cell neighbor, string currentSide, string neighborSide, int nx, int ny, int nz)
    {
        HashSet<string> allowedNames = new HashSet<string>();
        foreach (ModulData currentModule in current.possibleModules)
        {
            foreach (string name in currentModule.GetValidNeighbours(currentSide))
                allowedNames.Add(name);
        }

        List<ModulData> validModules = neighbor.possibleModules
            .Where(m => allowedNames.Contains(m.name))
            .Where(m => IsConstraintSatisfied(m, nx, ny, nz))
            .ToList();

        bool changed = validModules.Count != neighbor.possibleModules.Count;
        neighbor.possibleModules = validModules;
        return changed;
    }

    private bool IsConstraintSatisfied(ModulData module, int x, int y, int z)
    {
        if (module.constrain_to == "bot" && y != 0)
            return false;

        if (module.constrain_from == "bot" && y == 0)
            return false;

        return true;
    }




    private void RunWFC()
    {
        const int maxAttempts = 100;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            contradictionFound = false;
            InitializeGrid();
            ApplyBorderConstraints();

            // Propagate seeded cells
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    for (int k = 0; k < length; k++)
                        if (grid[i, j, k].collapsed)
                            Propagate(i, j, k);

            while (!Finished())
            {
                Vector3Int pos = FindLowestEntrophy();
                CollapseCell(pos.x, pos.y, pos.z);
                if (contradictionFound) break;
            }

            if (!contradictionFound) return; // success
            Debug.LogWarning($"Contradiction on attempt {attempt + 1}, retrying...");
        }

        Debug.LogError($"WFC failed after {maxAttempts} attempts");
    }

    private void CollapseCell(int x, int y, int z)
    {
        Cell cell = grid[x, y, z];
        if (cell.collapsed) return;

        if (cell.possibleModules.Count == 0)
        {
            Debug.LogWarning($"Contradiction at ({x},{y},{z})");
            contradictionFound = true;
            return;
        }

        int totalWeight = cell.possibleModules.Sum(m => m.weight);
        int roll = rnd.Next(0, totalWeight);
        int cumulative = 0;
        ModulData chosen = cell.possibleModules[0];

        foreach (ModulData m in cell.possibleModules)
        {
            cumulative += m.weight;
            if (roll < cumulative)
            {
                chosen = m;
                break;
            }
        }

        cell.possibleModules = new List<ModulData> { chosen };
        cell.collapsed = true;
        Propagate(x, y, z);
    }

    private Vector3Int FindLowestEntrophy()
    {
        int lowestEntrophy = Int32.MaxValue;
        List<Vector3Int> candidateCells = new List<Vector3Int>();
        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < height; ++j)
            {
                for (int k = 0; k < length; ++k)
                {
                    if (grid[i, j, k].collapsed) continue;

                    int entropy = grid[i, j, k].Entropy();

                    if (entropy < lowestEntrophy)
                    {
                        lowestEntrophy = entropy;
                        candidateCells.Clear(); 
                        candidateCells.Add(new Vector3Int(i, j, k));
                    }
                    else if (entropy == lowestEntrophy)
                    {
                        candidateCells.Add(new Vector3Int(i, j, k));
                    }
                }
            }
        }

        if(candidateCells.Count > 0)
        {
            return candidateCells[rnd.Next(0, candidateCells.Count)];
        }
           

        return new Vector3Int(-1,-1,-1);
    }

    private bool Finished()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int k = 0; k < length; k++)
                {
                    if (!grid[i, j, k].collapsed)
                        return false;
                }
            }
        }
        Debug.Log("All cells have been collapsed");
        return true;
    }

    private void Build()
    {
        float tileSize = 1f;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < length; z++)
                {
                    ModulData m = grid[x, y, z].possibleModules[0];
                    if (m.name.ToLower() == "p-1") continue;

                    Vector3 position = new Vector3(x * tileSize, y * tileSize, z * tileSize);

                    // Convert mesh_rotation (0-3) into 0, 90, 180, 270 degrees on Y axis
                    Quaternion rotation = Quaternion.Euler(0, m.mesh_rotation * 90f, 0);

                    Instantiate(m.prefab, position, rotation);
                }
    }

    public Cell[,,] GetGrid()
    {
        return grid;
    }



}

