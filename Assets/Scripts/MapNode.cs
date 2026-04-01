using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapNode 
{
   public MapNode[] neighbors;  
   public List<Vector3> spawnPositions;
   
   public GameObject map;

   public void SetNeighbors(MapNode[] neighbors)
   {
      this.neighbors = neighbors;
   }

   public void SetSpawnPositions(List<Vector3> spawnPositions)
   {
      this.spawnPositions = spawnPositions;
   }

   public void EnterRoom()
   {
      EnableRoom();
   }

   public void ExitRoom()
   {
      DisableRoom();
   }

   private void EnableRoom()
   {
      map.SetActive(true);
   }

   private void DisableRoom()
   {
      map.SetActive(false);
   }
}
