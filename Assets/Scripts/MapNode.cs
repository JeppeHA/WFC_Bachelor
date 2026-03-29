using System.Collections.Generic;
using UnityEngine;

public class MapNode 
{
   private MapNode[] neighbors;

   private List<Vector3> spawnPositions;
   
   public GameObject map;

   public void SetNeighbors(MapNode[] neighbors)
   {
      this.neighbors = neighbors;
   }

   public void SetSpawnPositions(List<Vector3> spawnPositions)
   {
      this.spawnPositions = spawnPositions;
   }
}
