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

    void Start()
    {
        CreateModules();
        
    }


    public void CreateModules()
    {
        for (int i = 0; i < numberOfLayers; i++)
        {
            for(int k = 0; i  <= layerObjects.Count; k++)
            {
                AddModule(null);
            }
            
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
                WFCTile module = CreateModule(i, layerObjects[j].prefab, indexs[0], indexs[1], indexs[4], indexs[5], 1);
                Debug.Log(module.prefab.name);
                AddModule(module);
            }
        }

    }


    private void PrintNeighbors(int[] neighborList)
    {
        foreach (int neighbor in neighborList)
        {
            Debug.Log(neighbor);
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

        currentFloorIndex = modules.Count - layerObjects.Count;

        int px;
        int nx;
        int pz;
        int nz;

        if (layerObject.name.ToLower().Contains("stairs"))
        {
            switch (layerObject.name[layerObject.name.Length - 1])
            {
                case '0':
                    neighborIndexs[0] = new int[] { currentFloorIndex };
                    neighborIndexs[1] = new int[] { modules.Count };
                    neighborIndexs[3] = new int[] {1 + currentFloorIndex, modules.Count };
                    neighborIndexs[4] = new int[] { 1 + currentFloorIndex, modules.Count };
                    break;
                case '1':
                    
                break;
                case '4':

                break;
                case '5':
                break;
            }
            if(neighborIndexs == null)
            {
                return null;
            }
            return neighborIndexs;
        }
        else
        {
            px = 2 + currentFloorIndex;
            nx = 4 + currentFloorIndex;
            pz = 1 + currentFloorIndex;
            nz = 3 + currentFloorIndex;
            for (int i = 0; i < neighborIndexs.Length; i++) {
                    if (i == 2 || i == 3)
                    {
                        neighborIndexs[i] = new int[] { -1 };
                        continue;
                    }
                    switch (i)
                    {
                    case 0:
                        neighborIndexs[i] = new int[] { currentFloorIndex, px, nx, nz };
                        break;
                    case 1:
                        neighborIndexs[i] = new int[] { currentFloorIndex, px, nz, pz };
                        break;
                    case 4:
                        neighborIndexs[i] = new int[] { currentFloorIndex, px, pz, nz };
                        break;
                    case 5:
                        neighborIndexs[i] = new int[] { currentFloorIndex, nx, pz, nz };
                        break;
                    }
                }
        }

            return neighborIndexs;
    }


    private WFCTile CreateModule(int layer, GameObject layerObject, int[] px, int[]nx, int[]pz, int[] xz, int weight)
    {
        WFCTile module = new WFCTile();
        module.prefab = layerObject;
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
