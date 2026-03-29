using UnityEngine;

[CreateAssetMenu(fileName = "WFCTile", menuName = "WFC/Tile")]
public class WFCModule : ScriptableObject
{
    public GameObject obj;
    public int layer;
    [Header("Neighbor Rules (which tile IDs can be placed in each direction)")]
    public int[] posXNeighbors; // +X (right)
    public int[] negXNeighbors; // -X (left)
    public int[] posYNeighbors; // +Y (up)
    public int[] negYNeighbors; // -Y (down)
    public int[] posZNeighbors; // +Z (forward)
    public int[] negZNeighbors; // -Z (back)

    [Header("Settings")]
    [Tooltip("Higher weight = more likely to be chosen")]
    public float weight = 1f;
}

/*
 * private bool PropagateAll(List<Vector3Int> starts)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();

        foreach (var pos in starts)
            queue.Enqueue(pos);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            WFCCell currentCell = grid[current.x, current.y, current.z];

            for (int d = 0; d < 6; d++)
            {
                if (d == 2 || d == 3) continue; // skip Y

                Vector3Int neighborPos = current + Directions[d];
                if (!InBounds(neighborPos)) continue;

                WFCCell neighbor = grid[neighborPos.x, neighborPos.y, neighborPos.z];
                if (neighbor.collapsed) continue;

                HashSet<int> allowed = new HashSet<int>();
                foreach (int moduleIdx in currentCell.possibleModules)
                foreach (int allowedNeighbor in GetNeighbors(modules[moduleIdx], d))
                    allowed.Add(allowedNeighbor);

                int before = neighbor.possibleModules.Count;
                neighbor.possibleModules.RemoveAll(t => !allowed.Contains(t));

                if (neighbor.IsContradiction) return false;

                if (neighbor.possibleModules.Count < before)
                    queue.Enqueue(neighborPos);
            }
        }

        return true;
    }
 */
