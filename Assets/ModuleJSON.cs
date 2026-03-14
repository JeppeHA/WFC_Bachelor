using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ModuleJson
{
    public string mesh_name;
    public int mesh_rotation;
    public string posX, negX, posY, negY, posZ, negZ;
    public string constrain_to, constrain_from;
    public int weight;
    public List<List<string>> valid_neighbours;
}
