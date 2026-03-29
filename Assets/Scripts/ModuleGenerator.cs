using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Generates WFC (Wave Function Collapse) modules across multiple layers,
/// including floor tiles, stairs, and transition pieces.
/// </summary>
public class ModuleGenerator : MonoBehaviour
{
    [Header("Layer Configuration")]
    public int numberOfLayers;
    public List<WFCModule> layerObjects; // Must follow ground-then-stairs order

    [Header("Module Data")]
    [SerializeField] private List<WFCModule> modules;
    [SerializeField] private List<WFCModule> transitions;
    
    private int currentFloorIndex = 0;
    

    private void Awake() => CreateModules();

    // ─── Module Creation ───────────────────────────────────────────────────────

    public void CreateModules()
    {
        CreateLayerModules();
        CreateTopFloorCap();
        GenerateTransitions();
        UpdateIndices();
    }

    private void CreateLayerModules()
    {
        for (int layer = 0; layer < numberOfLayers; layer++)
        {
            foreach (WFCModule module in layerObjects)
            {
                if (layer == 0)
                {
                    AddModule(module);
                    continue;
                }

                int[][] neighbors = AssignNeighbors(layer, module);
                if (neighbors == null) continue;

                Debug.Log($"Neighbor count: {neighbors.Length}");
                WFCModule temp_module = CreateModule(layer, module.obj, neighbors[0], neighbors[1], neighbors[4], neighbors[5], module.weight);
                AddModule(temp_module);
            }
        }
    }

    /// <summary>Adds a final floor cap above the topmost layer.</summary>
    private void CreateTopFloorCap()
    {
        Debug.Log("Creating top floor cap");
        WFCModule baseTile = layerObjects[0];
        int[][] neighbors = AssignNeighbors(numberOfLayers, baseTile);
        WFCModule capModule = CreateModule(numberOfLayers, baseTile.obj, neighbors[0], neighbors[1], neighbors[4], neighbors[5], baseTile.weight);
        AddModule(capModule);
    }

    private void GenerateTransitions()
    {
        for (int layer = 0; layer < numberOfLayers; layer++)
        {
            foreach (WFCModule transition in transitions)
            {
                int[][] neighbors = AssignDoorNeighbors(layer);
                WFCModule module = CreateModule(layer, transition.obj, neighbors[0], neighbors[1], neighbors[4], neighbors[5], transition.weight);
                AddModule(module);
            }
        }
    }

    private void UpdateIndices()
    {
        int amountOfTransitions = modules.Count - numberOfLayers;
        int index = amountOfTransitions + 1;
        for (int i = 0; i < modules.Count; i+=3)
        {
            if(i == 0)
                continue;
            if(i >= layerObjects.Count * numberOfLayers)
                continue;
            
            int[][] temp = new int[4][];
            temp[0] = modules[i].posXNeighbors;
            temp[1] = modules[i].negXNeighbors;
            temp[2] = modules[i].posZNeighbors;
            temp[3] = modules[i].negZNeighbors;
            
            int[] indexs = {index};
          
            modules[i].posXNeighbors = modules[i].posXNeighbors.Union(indexs).ToArray();
            modules[i].negXNeighbors = modules[i].negXNeighbors.Union(indexs).ToArray();
            modules[i].posZNeighbors = modules[i].posZNeighbors.Union(indexs).ToArray();
            modules[i].negZNeighbors = modules[i].negZNeighbors.Union(indexs).ToArray();
            index++;

        }
    }

    // ─── Neighbor Assignment ───────────────────────────────────────────────────
    
    private int[][] AssignNeighbors(int layer, WFCModule tile)
    {
        string tileName = tile.name.ToLower();

        if (tileName.Contains("z")) return AssignZStairNeighbors(layer);
        if (tileName.Contains("x")) return AssignXStairNeighbors(layer);
        return AssignFloorNeighbors(layer);
    }

    private int[][] AssignZStairNeighbors(int layer)
    {
        currentFloorIndex = layerObjects.Count * layer;
        Debug.Log($"Z-Stair floor index: {currentFloorIndex}");

        int nextFloor = currentFloorIndex + layerObjects.Count;

        return new int[][]
        {
            new[] { currentFloorIndex + 1, nextFloor },  // +X
            new[] { currentFloorIndex + 1, nextFloor },  // -X
            new[] { -1 },                                // +Y
            new[] { -1 },                                // -Y
            new[] { nextFloor },                         // +Z
            new[] { currentFloorIndex },                 // -Z
        };
    }

    private int[][] AssignXStairNeighbors(int layer)
    {
        currentFloorIndex = layerObjects.Count * layer;

        int nextFloor = currentFloorIndex + layerObjects.Count;

        return new int[][]
        {
            new[] { nextFloor },                                  // +X
            new[] { currentFloorIndex },                          // -X
            new[] { -1 },                                         // +Y
            new[] { -1 },                                         // -Y
            new[] { currentFloorIndex + 2, nextFloor },           // +Z
            new[] { currentFloorIndex + 2, nextFloor },           // -Z
        };
    }

    private int[][] AssignFloorNeighbors(int layer)
    {
        currentFloorIndex = layerObjects.Count * layer;

        int nextFloor  = currentFloorIndex + layerObjects.Count;
        int prevFloor  = currentFloorIndex - layerObjects.Count;

        int[] forwardNeighbors = null;
        int[] backwardNeighbors = new []{ currentFloorIndex, prevFloor, currentFloorIndex - 1, currentFloorIndex - 2 };

        if (layer >= numberOfLayers)
        {
            forwardNeighbors = new []{ currentFloorIndex };
        }
        else
        {
            forwardNeighbors = new []{ currentFloorIndex, nextFloor,  currentFloorIndex + 2, currentFloorIndex + 1 };
           
        }
        
        

        return new int[][]
        {
            forwardNeighbors,   // +X
            backwardNeighbors,  // -X
            new[] { -1 },       // +Y
            new[] { -1 },       // -Y
            forwardNeighbors,   // +Z
            backwardNeighbors,  // -Z
        };
    }

    private int[][] AssignDoorNeighbors(int layer)
    {
        currentFloorIndex = layerObjects.Count * layer;

        int[] sameFloor = { currentFloorIndex };

        return new int[][]
        {
            sameFloor,    // +X
            sameFloor,    // -X
            new[] { -1 }, // +Y
            new[] { -1 }, // -Y
            sameFloor,    // +Z
            sameFloor,    // -Z
        };
    }
    
    // Module factory

    private WFCModule CreateModule(int layer, GameObject obj, int[] posX, int[] negX, int[] posZ, int[] negZ, float weight)
    {
        Debug.Log($"Creating module for layer {layer}");

        WFCModule module = ScriptableObject.CreateInstance<WFCModule>();
        module.layer         = layer;
        module.obj           = obj;
        module.posXNeighbors = posX;
        module.negXNeighbors = negX;
        module.posYNeighbors = new[] { -1 };
        module.negYNeighbors = new[] { -1 };
        module.posZNeighbors = posZ;
        module.negZNeighbors = negZ;
        module.weight        = weight;
        module.name          = $"{obj.name}_{layer}";

        return module;
    }

    private void AddModule(WFCModule module)     => modules.Add(module);
    private void AddTransition(WFCModule module) => transitions.Add(module);

    public List<WFCModule> GetModules() => modules;
}