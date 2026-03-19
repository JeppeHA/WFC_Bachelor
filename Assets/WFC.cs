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
        Debug.Log($"Loaded {Modules.Count} tiles");

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

        // First pass: fill every cell with all modules
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                for (int k = 0; k < length; k++)
                    grid[i, j, k] = new Cell(Modules);

       for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                for (int k = 0; k < length; k++)
                    Propagate(i, j, k);
    }
    private void Propagate(int x, int y, int z)
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
    }

    private void CheckNeighbor(
    int cx, int cy, int cz,
    int nx, int ny, int nz,
    string currentSide,
    string neighborSide,
    Queue<Vector3Int> queue)
    {
        // Check bounds
        if (nx < 0 || nx >= width ||
            ny < 0 || ny >= height ||
            nz < 0 || nz >= length)
        {
            return;
        }

        Cell current = grid[cx, cy, cz];
        Cell neighbor = grid[nx, ny, nz];

        bool changed = FilterNeighbor(current, neighbor, currentSide, neighborSide);

        if (changed)
        {
            queue.Enqueue(new Vector3Int(nx, ny, nz));
        }
    }

    private bool FilterNeighbor(Cell current, Cell neighbor, string currentSide, string neighborSide)
    {

        // Collect every name that ANY current module allows on this face
        HashSet<string> allowedNames = new HashSet<string>();

        

        foreach (ModulData currentModule in current.possibleModules)
        {
           // Debug.Log($"current module: " + currentModule);
           // Debug.Log($"Get valid neighbours: " + currentModule.GetValidNeighbours(currentSide));
            foreach (string name in currentModule.GetValidNeighbours(currentSide))
                allowedNames.Add(name);
        }

        // Keep only neighbor modules whose name is in that allowed set
        List<ModulData> validModules = neighbor.possibleModules
            .Where(m => allowedNames.Contains(m.name))
            .ToList();

        bool changed = validModules.Count != neighbor.possibleModules.Count;
        neighbor.possibleModules = validModules;
        return changed;
    }




    private void RunWFC()
    {
        const int maxAttempts = 1000;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            contradictionFound = false;
            InitializeGrid();

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
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < length; z++)
                {
                    ModulData m = grid[x, y, z].possibleModules[0];

                   if (m.name.ToLower() == "p-1")
                        continue;

                    Renderer r = m.prefab.GetComponent<Renderer>();
                    if(r  == null)
                    {
                        r = m.prefab.GetComponentInChildren<Renderer>();
                    }
                    Vector3 position = new Vector3(x, y, z);
                    /*if(m.name.ToLower() != "p-1")
                    {
                        Vector3 size = r.bounds.size;

                       position = new Vector3(
                            x * size.x,
                            y * size.y,
                            z * size.z
                        );
                    }*/

                    

                    Instantiate(m.prefab, position, Quaternion.identity);
                }
    }


    public Cell[,,] GetGrid()
    {
        return grid;
    }



}

