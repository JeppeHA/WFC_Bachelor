# Simple 3D Wave Function Collapse — Unity

## Files

| File | Purpose |
|------|---------|
| `WFCTile.cs` | ScriptableObject — defines a tile prefab + its 6-direction neighbor rules |
| `WFCCell.cs` | Internal cell class — tracks the superposition and entropy |
| `WFCGenerator.cs` | MonoBehaviour — runs Observe → Collapse → Propagate loop |
| `WFCExampleSetup.cs` | Editor helper — generates 3 starter tile assets |

---

## Quick Start

### 1. Import scripts
Drop all `.cs` files into `Assets/Scripts/` (or any folder).

### 2. Create Tile assets
Open **GameObject › WFC › Create Example Tiles** to auto-generate
`Air`, `Grass`, and `Dirt` tile assets under `Assets/WFC/Tiles/`.

Or create your own:
- `Assets › Create › WFC › Tile`
- Assign a **Prefab** and set **neighbor arrays**.

### 3. Set up the Generator
- Create an empty GameObject, add **WFCGenerator**.
- Set `Grid X / Y / Z` and `Cell Size`.
- Expand the `Tiles` array and assign your tile assets **in index order**.
- Press ▶ Play — generation runs automatically.

You can also right-click the component and choose **Generate** or **Clear**
while in Play mode.

---

## Neighbor Rule Format

Each tile has 6 `int[]` arrays — one per axis direction:

```
posX (+X / right)    negX (-X / left)
posY (+Y / up)       negY (-Y / down)
posZ (+Z / forward)  negZ (-Z / back)
```

Fill each array with the **tile indices** that are allowed to sit next to
this tile in that direction.

### Example — 3-tile terrain

```
Index 0 = Air    Index 1 = Grass    Index 2 = Dirt

Air.posY   = { 0 }       ← air above air
Air.negY   = { 0, 1 }    ← air or grass below air
Air.posX   = { 0 }       ← air beside air

Grass.posY = { 0 }       ← air above grass
Grass.negY = { 2 }       ← dirt below grass
Grass.posX = { 1 }       ← grass beside grass

Dirt.posY  = { 1, 2 }    ← grass or dirt above dirt
Dirt.negY  = { 2 }       ← dirt below dirt
Dirt.posX  = { 2 }       ← dirt beside dirt
```

---

## Algorithm Steps

```
1. INITIALISE  — every cell holds all tile indices (full superposition)
2. OBSERVE     — pick the uncollapsed cell with the lowest Shannon entropy
3. COLLAPSE    — weighted-random select one tile for that cell
4. PROPAGATE   — BFS removes now-illegal options from neighbours (AC-3)
5. REPEAT      — back to step 2 until all cells are collapsed
6. CONTRADICTION? — restart from step 1 (up to maxRetries times)
```

---

## Inspector Options

| Property | Description |
|----------|-------------|
| `gridX/Y/Z` | Grid dimensions in tiles |
| `cellSize` | World-space size of each cell |
| `tiles` | Ordered array of WFCTile assets |
| `generateOnStart` | Run automatically on Play |
| `stepByStep` | Slow-motion debug mode |
| `stepDelay` | Seconds between steps in debug mode |
| `maxRetries` | How many times to retry on contradiction |

---

## Tips

- **Contradictions** usually mean your rules don't cover all adjacency cases.
  Enable `stepByStep` to watch propagation and spot the gap.
- **Weight** controls how often a tile is picked. Increase `Air.weight` for
  more open space, increase `Dirt.weight` for denser underground sections.
- For larger grids (32³+) consider running generation in a background thread
  and only calling `SpawnTiles()` on the main thread.
