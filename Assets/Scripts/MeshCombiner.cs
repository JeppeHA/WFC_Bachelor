using System.Collections.Generic;
using UnityEngine;

public class MeshCombiner : MonoBehaviour
{
    // Source Meshes you want to combine
    [SerializeField] private List<MeshFilter> listMeshFilter;

    // Make a new mesh to be the target of the combine operation
    [SerializeField] private MeshFilter TargetMesh;

    [ContextMenu("Combine Meshes")]
    private void CombineMesh()
    {
        Debug.Log("Combine Meshes!!!");
        //Make an array of CombineInstance.
        var combine = new CombineInstance[listMeshFilter.Count];
        
        //Set Mesh And their Transform to the CombineInstance
        for (int i = 0; i < listMeshFilter.Count; i++)
        {
            combine[i].mesh = listMeshFilter[i].sharedMesh;
            combine[i].transform = listMeshFilter[i].transform.localToWorldMatrix;
        }

        // Create a Empty Mesh
        var mesh = new Mesh();

        //Call targetMesh.CombineMeshes and pass in the array of CombineInstances.
        mesh.CombineMeshes(combine);

        //Assign the target mesh to the mesh filter of the combination game object.
        TargetMesh.mesh = mesh;

        // Print Results
        print($"<color=#20E7B0>Combine Meshes was Successful!</color>");
    }
    
    public void Combine()
    {
        CombineMesh();
    }
    public void AddMeshes(MeshFilter mesh)
    {
        listMeshFilter.Add(mesh);
    }
}
