using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class ModuleGenerator : MonoBehaviour
{
    public int numberOfLayers;

    [SerializeField]
    private List<WFCTile> modules;

    public List<WFCTile> layerObjects;

    private int currentFloorIndex = 0;

    // layerObjects needs to to follow an ground then stairs order in layerobjects
    //First layer is defined as scriptable objects in the inspector

    private void Awake()
    {
        CreateModules();
    }


    public void CreateModules()
    {
        for (int i = 0; i < numberOfLayers; i++)
        {

            
            for (int j = 0; j < layerObjects.Count; j++)
            {
                if (i == 0)
                {
                    AddModule(layerObjects[j]);
                    continue;
                }
                int[][] indexs = AssingNeighbors(i, layerObjects[j]);
                if (indexs == null)
                {
                    continue;
                }
                
                
                WFCTile module = CreateModule(i, layerObjects[j].obj, indexs[0], indexs[1], indexs[4], indexs[5], 1);
                Debug.Log(module.obj.name);
                AddModule(module);
            }

            if (i == numberOfLayers - 1)
            {
                int[][] lastFloorIndexs = AssingNeighbors(i + 1, layerObjects[0]);
                WFCTile lastModule = CreateModule(i+ 1, layerObjects[0].obj, lastFloorIndexs[0], lastFloorIndexs[1], lastFloorIndexs[4], lastFloorIndexs[5], 5); 
                AddModule(lastModule);
            }
            
        }

    }


    /*
     * Objects that rotate needs thier name to end with thier rotation 0=0 degrees 1=90 degrees...
     **/


    // [0][] px
    // [1][] nx
    // [4][] pz
    // [5][] nz

    private int[][] AssingNeighbors(int layer, WFCTile layerObject)
    {
        int[][] neighborIndexs = new int[6][];

        if (layerObject.name.ToLower().Contains("z"))
        
            neighborIndexs = AssignZStairs(layer);
        
        else if (layerObject.name.ToLower().Contains("x"))
            neighborIndexs = AssingXZStairs(layer);    
        else
            neighborIndexs = AssignFloor(layer);
            
            return neighborIndexs;
    }

    private int[][] AssignZStairs(int layer)
    {
        int[][] neighborIndexs = new int[6][];
        currentFloorIndex = layerObjects.Count * layer;
        Debug.Log("CurrentFloorIndex: " + currentFloorIndex);
        neighborIndexs[0] = new int[] { currentFloorIndex + 1, currentFloorIndex + layerObjects.Count };
        neighborIndexs[1] = new int[] { currentFloorIndex + 1, currentFloorIndex + layerObjects.Count };
        neighborIndexs[2] = new int[] { -1 };
        neighborIndexs[3] = new int[] { -1 };
        neighborIndexs[4] = new int[] { currentFloorIndex + (layerObjects.Count) };
        neighborIndexs[5] = new int[] { currentFloorIndex  };

        return neighborIndexs;
    }

    private int[][] AssingXZStairs(int layer)
    {
        int[][] neighborIndexs = new int[6][];
        currentFloorIndex = layerObjects.Count * layer;
    
        neighborIndexs[0] = new int[] { currentFloorIndex + layerObjects.Count };
        neighborIndexs[1] = new int[] { currentFloorIndex };
        neighborIndexs[2] = new int[] { -1 };
        neighborIndexs[3] = new int[] { -1 };
        neighborIndexs[4] = new int[] { currentFloorIndex + 2,  currentFloorIndex + layerObjects.Count };
        neighborIndexs[5] = new int[] { currentFloorIndex + 2,  currentFloorIndex + layerObjects.Count};

        return neighborIndexs;
    }

    private int[][] AssignFloor(int layer)
    {
        int[][] neighborIndexs = new int[6][];
        currentFloorIndex = layerObjects.Count * layer;
    
        neighborIndexs[0] = new int[] { currentFloorIndex, currentFloorIndex - 2, currentFloorIndex + 2  };
        neighborIndexs[1] = new int[] { currentFloorIndex, currentFloorIndex - 1, currentFloorIndex - layerObjects.Count};
        neighborIndexs[2] = new int[] { -1 };
        neighborIndexs[3] = new int[] { -1 };
        neighborIndexs[4] = new int[] { currentFloorIndex, currentFloorIndex - 1, currentFloorIndex + 1 };
        neighborIndexs[5] = new int[] { currentFloorIndex, currentFloorIndex -2, currentFloorIndex - layerObjects.Count  };

        return neighborIndexs;
    }

    private WFCTile CreateModule(int layer, GameObject layerObject, int[] px, int[]nx, int[]pz, int[] xz, int weight)
    {
        Debug.Log("Layer: "  + layer);
        WFCTile module = ScriptableObject.CreateInstance<WFCTile>();
        module.layer = layer;   
        module.obj = layerObject;
        module.posXNeighbors = px;
        module.negXNeighbors = nx;
        module.posYNeighbors = new int[1] { -1 };
        module.negYNeighbors = new int[1] { -1 };
        module.posZNeighbors = pz;
        module.negZNeighbors = xz;
        module.weight = weight;
        module.name = layerObject.name +"_" +layer; 
        return module;
    }

    private void AddModule(WFCTile module)
    {
        modules.Add(module);
    }

    public List<WFCTile> GetModules()
    {
        return modules;
    }
 

}
