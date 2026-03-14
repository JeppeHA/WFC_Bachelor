using System.Collections.Generic;
using UnityEngine;


public class Module
{
    public string name;
    public GameObject prefab;
    public List<string> pX = new List<string>();
    public List<string> nX = new List<string>();
    public List<string> pY = new List<string>();
    public List<string> nY = new List<string>();
    public List<string> pZ = new List<string>();
    public List<string> nZ = new List<string>();

    public List<string> GetSide(Module module, string side)
    {
        switch (side)
        {
            case "pX": return module.pX;
            case "nX": return module.nX;
            case "pY": return module.pY;
            case "nY": return module.nY;
            case "pZ": return module.pZ;
            case "nZ": return module.nZ;
        }
        return new List<string>();
    }
}