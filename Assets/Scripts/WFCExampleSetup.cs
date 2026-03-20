using UnityEngine;

/// <summary>
/// Optional helper – creates example WFCTile assets in the Editor
/// for a simple 3-tile terrain (sky / ground surface / underground).
///
/// Usage: GameObject > WFC > Create Example Tiles
/// Then assign the generated assets to WFCGenerator.tiles[].
///
/// Tile index reference:
///   0 = Air
///   1 = Grass (surface)
///   2 = Dirt  (underground)
/// </summary>
#if UNITY_EDITOR
using UnityEditor;

public static class WFCExampleSetup
{
    [MenuItem("GameObject/WFC/Create Example Tiles", false, 10)]
    static void CreateExampleTiles()
    {
        string folder = "Assets/WFC/Tiles";
        System.IO.Directory.CreateDirectory(folder);

        // ── Tile 0: Air ───────────────────────────────────────────────────────
        // Air can be next to air on all sides, and above grass on the Y- side.
        WFCTile air = ScriptableObject.CreateInstance<WFCTile>();
        air.weight = 8f;
        air.posXNeighbors = new[] { 0 };        // air | air
        air.negXNeighbors = new[] { 0 };
        air.posYNeighbors = new[] { 0 };        // air above air
        air.negYNeighbors = new[] { 0, 1 };     // grass or air below
        air.posZNeighbors = new[] { 0 };
        air.negZNeighbors = new[] { 0 };
        AssetDatabase.CreateAsset(air, $"{folder}/Air.asset");

        // ── Tile 1: Grass ─────────────────────────────────────────────────────
        // Grass connects horizontally to grass, has air above, dirt below.
        WFCTile grass = ScriptableObject.CreateInstance<WFCTile>();
        grass.weight = 3f;
        grass.posXNeighbors = new[] { 1 };      // grass | grass
        grass.negXNeighbors = new[] { 1 };
        grass.posYNeighbors = new[] { 0 };      // air above grass
        grass.negYNeighbors = new[] { 2 };      // dirt below grass
        grass.posZNeighbors = new[] { 1 };
        grass.negZNeighbors = new[] { 1 };
        AssetDatabase.CreateAsset(grass, $"{folder}/Grass.asset");

        // ── Tile 2: Dirt ──────────────────────────────────────────────────────
        // Dirt connects horizontally to dirt, has grass or dirt above, dirt below.
        WFCTile dirt = ScriptableObject.CreateInstance<WFCTile>();
        dirt.weight = 4f;
        dirt.posXNeighbors = new[] { 2 };
        dirt.negXNeighbors = new[] { 2 };
        dirt.posYNeighbors = new[] { 1, 2 };    // grass or dirt above
        dirt.negYNeighbors = new[] { 2 };       // dirt below
        dirt.posZNeighbors = new[] { 2 };
        dirt.negZNeighbors = new[] { 2 };
        AssetDatabase.CreateAsset(dirt, $"{folder}/Dirt.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created example tiles in {folder}. " +
                  "Assign them to WFCGenerator.tiles[] in order: Air(0), Grass(1), Dirt(2).");
    }
}
#endif
