using UnityEngine;

[CreateAssetMenu(fileName = "WFCTile", menuName = "WFC/Tile")]
public class WFCTile : ScriptableObject
{
    public GameObject prefab;

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
