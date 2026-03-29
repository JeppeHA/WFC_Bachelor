using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]   
public class Cell
{
    public bool collapsed = false;

    public List<ModulData> possibleModules;

    public Cell(List<ModulData> possibleModules)
    {
        this.possibleModules = new List<ModulData>(possibleModules);
    }

    public int Entropy()
    {
        return possibleModules.Count;
    }

    public ModulData GetCollapsedModule()
    {
        if (possibleModules.Count == 1)
            return possibleModules[0];

        return null;
    }

}