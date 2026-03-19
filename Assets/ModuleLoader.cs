using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;

public class ModuleLoader : MonoBehaviour
{

    [System.Serializable]
    public class PrefabEntry
    {
        public string meshName;      
        public GameObject prefab;    
    }

    public List<PrefabEntry> prefabMap;  

    public List<ModulData> LoadModules()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("MyPrototype");
        Dictionary<string, ModuleJson> raw =
            JsonConvert.DeserializeObject<Dictionary<string, ModuleJson>>(jsonFile.text);

        List<ModulData> result = new List<ModulData>();
        foreach (var kvp in raw)
        {
            ModuleJson data = kvp.Value;
            ModulData tile = ScriptableObject.CreateInstance<ModulData>();
            tile.name = kvp.Key;
            tile.pX = data.posX;
            tile.nX = data.negX;
            tile.pY = data.posY;
            tile.nY = data.negY;
            tile.pZ = data.posZ;
            tile.nZ = data.negZ;
            tile.weight = data.weight;
            tile.constrain_to = data.constrain_to ?? "";
            tile.constrain_from = data.constrain_from ?? "";

            tile.valid_neighbours = new List<string>[6];
            for (int i = 0; i < 6; i++)
                tile.valid_neighbours[i] = i < data.valid_neighbours.Count
                    ? data.valid_neighbours[i]
                    : new List<string>();

            // Look up the prefab by mesh_name
            PrefabEntry entry = prefabMap.Find(p => p.meshName == data.mesh_name);
            if (entry != null)
                tile.prefab = entry.prefab;
            else
                Debug.LogWarning($"No prefab found for mesh_name: {data.mesh_name}");

            result.Add(tile);
        }


        HashSet<string> knownNames = new HashSet<string>();
        foreach (ModulData m in result)
            knownNames.Add(m.name);

        // Validate — catch dangling references and null slots before WFC runs
        foreach (ModulData m in result)
        {
            for (int i = 0; i < m.valid_neighbours.Length; i++)
            {
                if (m.valid_neighbours[i] == null)
                {
                    Debug.LogError($"Module '{m.name}' side {i} has a null neighbour list — filling empty");
                    m.valid_neighbours[i] = new List<string>();
                    continue;
                }
                foreach (string neighbourName in m.valid_neighbours[i])
                {
                    if (!knownNames.Contains(neighbourName))
                        Debug.LogError($"Module '{m.name}' side {i} references unknown module '{neighbourName}'");
                }
            }
        }
            return result;
    }
}