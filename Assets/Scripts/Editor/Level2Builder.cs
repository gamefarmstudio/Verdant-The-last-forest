using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public static class Level2Builder
{
    // ── Tile asset paths (matching Level 1) ──────────────────────────────────────
    const string TILE_PATH   = "Assets/Game Assets/Tilemap/Tilemap brush/Tilemap_color4_9.asset";
    const string TILE_GRASS  = "Assets/Game Assets/Tilemap/Tilemap brush/tile_061_0.asset";
    const string TILE_GRASS2 = "Assets/Game Assets/Tilemap/Tilemap brush/Tilemap_color5_9.asset";
    const string TILE_BORDER = "Assets/Game Assets/Tilemap/Tilemap brush/tile_045_0.asset";

    // ── Level bounds ─────────────────────────────────────────────────────────────
    const int MAP_XMIN = -14, MAP_XMAX = 13;
    const int MAP_YMIN =  -9, MAP_YMAX =  4;

    // ── Zigzag path layout (2-tile-wide lanes) ───────────────────────────────────
    //
    //   SPAWN(-13,1.5) →→→→→→→→→→→→→→ TP1(5, 1.5)
    //                                      ↓
    //                        TP2(5,-2.5) ←← (right col x=4..5, y=-3..2)
    //                          ↓
    //      TP3(-10,-2.5) ←←←←←← (mid row y=-3..-2, x=-11..5)
    //          ↓
    //      TP4(-10,-6.5) →→→→→→→→→→→→→→ END(12,-6.5)
    //        (bot row y=-7..-6, x=-11..11)

    [MenuItem("Verdant/Build Level 2")]
    public static void BuildLevel2()
    {
        var tilePath   = AssetDatabase.LoadAssetAtPath<TileBase>(TILE_PATH);
        var tileGrass  = AssetDatabase.LoadAssetAtPath<TileBase>(TILE_GRASS);
        var tileGrass2 = AssetDatabase.LoadAssetAtPath<TileBase>(TILE_GRASS2);
        var tileBorder = AssetDatabase.LoadAssetAtPath<TileBase>(TILE_BORDER);

        if (tilePath == null) { Debug.LogError("[Level2Builder] Missing: " + TILE_PATH); return; }
        if (tileGrass == null) { Debug.LogError("[Level2Builder] Missing: " + TILE_GRASS); return; }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ────────────────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        var cam   = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor  = new Color(0.13f, 0.54f, 0.13f);
        camGO.tag                = "MainCamera";
        camGO.transform.position = new Vector3(-0.5f, -2f, -10f);

        // ── Global Light 2D ───────────────────────────────────────────────────────
        var lightGO = new GameObject("Global Light 2D");
        var light   = lightGO.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        // ── Grid + 3 Tilemap layers ───────────────────────────────────────────────
        var gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();
        gridGO.transform.position = Vector3.zero;

        var pathCells = BuildPathCells();

        // Layer 1 — "Tilemap - Path" (order 0): dirt path only
        var tmPath = CreateTilemapLayer(gridGO, "Tilemap - Path", "Untagged", 0);
        foreach (var cell in pathCells)
            tmPath.SetTile(cell, tilePath);

        // Layer 2 — "Tilemap - Map" (order 1): grass background everywhere except path
        var tmMap = CreateTilemapLayer(gridGO, "Tilemap - Map", "Untagged", 1);
        FillBackground(tmMap, tileGrass, tileGrass2, pathCells);

        // Layer 3 — "Tilemap - Border" (order 1): edge/cliff tiles bordering the path
        if (tileGrass2 != null && tileBorder != null)
        {
            var tmBorder = CreateTilemapLayer(gridGO, "Tilemap - Border", "Untagged", 1);
            PaintBorders(tmBorder, tileGrass2, tileBorder, pathCells);
        }

        // ── Pathfinding object ────────────────────────────────────────────────────
        var pfGO = new GameObject("PathfindingManager");
        pfGO.AddComponent<Grid2D>();
        pfGO.AddComponent<Pathfinding2D>();

        // ── TurnPoints ────────────────────────────────────────────────────────────
        // Path lane centers: top row y=1.5, right col x=4.5, mid row y=-2.5, left col x=-10.5, bot row y=-6.5
        var tpRoot = new GameObject("Turnpoints");
        CreateTurnPoint(tpRoot, "TurnPoint_1", new Vector3( 4.5f,  1.5f, 0), Vector2.down);
        CreateTurnPoint(tpRoot, "TurnPoint_2", new Vector3( 4.5f, -2.5f, 0), Vector2.left);
        CreateTurnPoint(tpRoot, "TurnPoint_3", new Vector3(-10.5f,-2.5f, 0), Vector2.down);
        CreateTurnPoint(tpRoot, "TurnPoint_4", new Vector3(-10.5f,-6.5f, 0), Vector2.right);

        // ── EnemySpawner ──────────────────────────────────────────────────────────
        var spawnerGO = new GameObject("EnemySpawnner");   // matches Level 1 spelling
        spawnerGO.transform.position = new Vector3(-13f, 1.5f, 0f);
        var spawner = spawnerGO.AddComponent<EnemySpawner>();
        SetField(spawner, "spawnInterval", 1.2f, nonPublic: true);

        // ── End zone ──────────────────────────────────────────────────────────────
        var endGO = new GameObject("EndZone");
        endGO.transform.position = new Vector3(12f, -6.5f, 0f);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Levels/Level2.unity");
        Debug.Log("[Level2Builder] Level 2 saved to Assets/Scenes/Levels/Level2.unity");
    }

    // ── Path cell set ─────────────────────────────────────────────────────────────

    static HashSet<Vector3Int> BuildPathCells()
    {
        var cells = new HashSet<Vector3Int>();
        AddRect(cells, -12,  1,  4,  2);   // top row
        AddRect(cells,   4, -3,  5,  2);   // right column (+ top corner)
        AddRect(cells, -11, -3,  5, -2);   // middle row (+ both corners)
        AddRect(cells, -11, -7, -10,-2);   // left column (+ mid corner)
        AddRect(cells, -11, -7, 11,  -6);  // bottom row
        return cells;
    }

    static void AddRect(HashSet<Vector3Int> cells, int xMin, int yMin, int xMax, int yMax)
    {
        for (int x = xMin; x <= xMax; x++)
            for (int y = yMin; y <= yMax; y++)
                cells.Add(new Vector3Int(x, y, 0));
    }

    // ── Tilemap painters ──────────────────────────────────────────────────────────

    static void FillBackground(Tilemap tm, TileBase grassA, TileBase grassB, HashSet<Vector3Int> skip)
    {
        for (int x = MAP_XMIN; x <= MAP_XMAX; x++)
        {
            for (int y = MAP_YMIN; y <= MAP_YMAX; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (skip.Contains(cell)) continue;
                // Alternate grass tiles in a checkerboard pattern for visual variety
                tm.SetTile(cell, (grassB != null && (x + y) % 3 == 0) ? grassB : grassA);
            }
        }
    }

    static void PaintBorders(Tilemap tm, TileBase tileA, TileBase tileB, HashSet<Vector3Int> pathCells)
    {
        var painted = new HashSet<Vector3Int>();
        var dirs = new[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (var cell in pathCells)
        {
            foreach (var dir in dirs)
            {
                var n = cell + dir;
                if (!pathCells.Contains(n) && !painted.Contains(n))
                {
                    // Use tileA on horizontal borders, tileB on vertical borders
                    tm.SetTile(n, (dir == Vector3Int.left || dir == Vector3Int.right) ? tileA : tileB);
                    painted.Add(n);
                }
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    static Tilemap CreateTilemapLayer(GameObject gridGO, string name, string tag, int sortOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(gridGO.transform, false);
        go.tag = tag;
        var tm = go.AddComponent<Tilemap>();
        var tr = go.AddComponent<TilemapRenderer>();
        tr.sortingOrder = sortOrder;
        return tm;
    }

    static void CreateTurnPoint(GameObject parent, string name, Vector3 pos, Vector2 direction)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        var tp = go.AddComponent<TurnPoint>();
        SetField(tp, "possibleDirections", new Vector2[] { direction }, nonPublic: false);
        SetField(tp, "turnRadius", 0.15f, nonPublic: false);
    }

    static void SetField(object obj, string fieldName, object value, bool nonPublic)
    {
        var flags = System.Reflection.BindingFlags.Instance |
                    (nonPublic ? System.Reflection.BindingFlags.NonPublic
                               : System.Reflection.BindingFlags.Public);
        obj.GetType().GetField(fieldName, flags)?.SetValue(obj, value);
    }
}
