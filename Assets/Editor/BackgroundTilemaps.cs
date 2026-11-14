using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using OooAdventure;

// Editor-only conversion. Run explicitly; importing the project does not rebuild the map.
[InitializeOnLoad]
public static class BackgroundTilemaps
{
    const string Request = "Library/BackgroundTilemaps.request";
    const string Report = "Library/BackgroundTilemaps.txt";
    const string ScenePath = "Assets/Scenes/Level01_FrozenForest.unity";
    const string TileFolder = "Assets/Tiles/Level01/Background";
    const string PaletteFolder = "Assets/Palettes/Level01";
    static readonly StringBuilder results = new StringBuilder();

    static BackgroundTilemaps()
    {
        if (File.Exists(Request)) EditorApplication.delayCall += Convert;
    }

    static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static Tile GetTile(Sprite sprite, bool far, Color color)
    {
        string path = TileFolder + "/" + (far ? "Far_" : "Near_") + sprite.name + ".asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile != null) return tile;
        tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = color;
        tile.transform = Matrix4x4.Scale(far ? new Vector3(3.5f, 4, 1) : new Vector3(1.6f, 1.8f, 1));
        tile.colliderType = Tile.ColliderType.None;
        tile.flags = TileFlags.None;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    static bool Close(Matrix4x4 a, Matrix4x4 b)
    {
        for (int i = 0; i < 16; i++) if (Mathf.Abs(a[i] - b[i]) > .0001f) return false;
        return true;
    }

    static Tilemap ConvertLayer(ParallaxLayer layer, Grid grid, bool far)
    {
        var sprites = layer.GetComponentsInChildren<SpriteRenderer>(true);
        Require(sprites.Length > 0, "No background sprites in " + layer.name);
        Require(sprites.All(s => s.enabled && s.gameObject.activeInHierarchy && s.drawMode == SpriteDrawMode.Simple), "Unsupported hidden or sliced background sprite.");
        Require(sprites.All(s => s.GetComponents<Component>().Length == 2 && s.transform.childCount == 0), "Background contains additional components; conversion stopped to preserve them.");
        var material = sprites[0].sharedMaterial;
        int sortingLayer = sprites[0].sortingLayerID, sortingOrder = sprites[0].sortingOrder;
        Require(sprites.All(s => s.sharedMaterial == material && s.sortingLayerID == sortingLayer && s.sortingOrder == sortingOrder), "Background layer has mixed rendering settings.");

        Undo.SetTransformParent(layer.transform, grid.transform, "Move background into Grid");
        Undo.RecordObject(layer.gameObject, "Rename background tilemap");
        layer.name = far ? "BackgroundFarTilemap" : "BackgroundNearTilemap";
        layer.gameObject.layer = LayerMask.NameToLayer("Background");
        var map = Undo.AddComponent<Tilemap>(layer.gameObject);
        var renderer = Undo.AddComponent<TilemapRenderer>(layer.gameObject);
        map.tileAnchor = Vector3.zero;
        renderer.sharedMaterial = material;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = sortingOrder;
        renderer.mode = TilemapRenderer.Mode.Individual;

        foreach (var source in sprites)
        {
            var cell = map.WorldToCell(source.transform.position);
            Require(!map.HasTile(cell), "Two background trees occupy " + cell + "; conversion stopped.");
            var flip = Matrix4x4.Scale(new Vector3(source.flipX ? -1 : 1, source.flipY ? -1 : 1, 1));
            var world = source.transform.localToWorldMatrix * flip;
            var cellLocal = map.CellToLocal(cell);
            var matrix = Matrix4x4.Translate(-cellLocal) * map.transform.worldToLocalMatrix * world;
            map.SetTile(cell, GetTile(source.sprite, far, source.color));
            map.SetTileFlags(cell, TileFlags.None);
            map.SetColor(cell, source.color);
            map.SetTransformMatrix(cell, matrix);
            var actualWorld = map.transform.localToWorldMatrix * Matrix4x4.Translate(cellLocal) * map.GetTransformMatrix(cell);
            Require(map.GetSprite(cell) == source.sprite && map.GetColor(cell) == source.color && Close(actualWorld, world), "Background appearance changed at " + cell);
        }
        Require(map.GetComponents<Collider2D>().Length == 0, "Background must not have physics colliders.");
        map.CompressBounds();
        foreach (var source in sprites) Undo.DestroyObjectImmediate(source.gameObject);
        results.AppendLine("PASS: " + layer.name + ": " + sprites.Length + " trees retain original sprites, world transforms, colors, material and sorting. Parallax factor " + layer.factor + ". No colliders.");
        return map;
    }

    static GameObject Palette(Tilemap source, bool far)
    {
        string name = far ? "FrozenForest_BackgroundFar" : "FrozenForest_BackgroundNear";
        string path = PaletteFolder + "/" + name + ".prefab";
        var palette = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (palette == null)
            palette = GridPaletteUtility.CreateNewPalette(PaletteFolder, name, GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Manual, Vector3.one, GridLayout.CellSwizzle.XYZ);
        var contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var map = contents.GetComponentInChildren<Tilemap>();
            map.ClearAllTiles();
            map.tileAnchor = Vector3.zero;
            map.GetComponent<TilemapRenderer>().sharedMaterial = source.GetComponent<TilemapRenderer>().sharedMaterial;
            var tiles = new HashSet<TileBase>();
            foreach (var cell in source.cellBounds.allPositionsWithin)
                if (source.HasTile(cell)) tiles.Add(source.GetTile(cell));
            int index = 0;
            foreach (var tile in tiles.OrderBy(t => t.name)) map.SetTile(new Vector3Int(index++ * 12, 0, 0), tile);
            map.CompressBounds();
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            results.AppendLine("PASS: " + name + " contains " + tiles.Count + " native tree tiles, spaced apart for picking.");
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static void VerifyBrush(Grid grid, Tilemap target, GameObject palette)
    {
        var paletteMap = palette.GetComponentInChildren<Tilemap>();
        var source = Vector3Int.zero;
        bool found = false;
        foreach (var cell in paletteMap.cellBounds.allPositionsWithin)
            if (paletteMap.HasTile(cell)) { source = cell; found = true; break; }
        Require(found, "Palette has no tiles to pick.");
        var probe = new Vector3Int(1000, 1000, 0);
        Require(!target.HasTile(probe), "Brush test position is occupied.");
        var brush = ScriptableObject.CreateInstance<GridBrush>();
        try
        {
            brush.Pick(palette.GetComponent<Grid>(), paletteMap.gameObject, new BoundsInt(source, Vector3Int.one), Vector3Int.zero);
            brush.Paint(grid, target.gameObject, probe);
            Require(target.GetTile(probe) == paletteMap.GetTile(source), "Native palette brush did not paint.");
            Require(Close(target.GetTransformMatrix(probe), paletteMap.GetTransformMatrix(source)) && target.GetColor(probe) == paletteMap.GetColor(source), "Painted tree lost its default size or tint.");
            brush.Erase(grid, target.gameObject, probe);
            Require(!target.HasTile(probe), "Native palette brush did not erase.");
        }
        finally
        {
            target.SetTile(probe, null);
            target.CompressBounds();
            UnityEngine.Object.DestroyImmediate(brush);
        }
        results.AppendLine("PASS: GridBrush picks, paints and erases on " + target.name + ", retaining palette scale and color. Test cell removed.");
    }

    [MenuItem("Tools/Ooo Adventure/Map/Convert Backgrounds to Tilemaps")]
    public static void Convert()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += Convert; return; }
        if (File.Exists(Request)) File.Delete(Request);
        results.Clear();
        int undoGroup = -1;
        try
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play Mode before editing the map.");
            var scene = EditorSceneManager.GetActiveScene();
            Require(scene.path == ScenePath, "Open Level01_FrozenForest first.");
            var grid = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Grid>(true)).Single();
            var oldWrapper = grid.transform.parent.Find("Background");
            var layers = oldWrapper == null ? new ParallaxLayer[0] : oldWrapper.GetComponentsInChildren<ParallaxLayer>(true);
            if (layers.Length == 0)
            {
                Require(grid.transform.Find("BackgroundFarTilemap") != null && grid.transform.Find("BackgroundNearTilemap") != null, "Cannot find the two background layers.");
                OpenFarPalette();
                File.WriteAllText(Report, "PASS: Backgrounds are already editable Tilemaps under Grid.");
                return;
            }
            Require(layers.Length == 2 && oldWrapper.childCount == 2, "Expected only two parallax layers in Background.");
            var far = layers.Single(l => l.name.Contains("Parallax Forest"));
            var near = layers.Single(l => l.name.Contains("Near Trees"));
            var ground = grid.transform.Find("GroundTilemap").GetComponent<Tilemap>();
            int groundTiles = ground.GetUsedTilesCount();
            var groundMatrix = ground.transform.localToWorldMatrix;
            EditorSceneManager.SaveScene(scene);
            string backup = "Library/BeforeBackgroundTilemaps/" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            File.Copy(ScenePath, backup + "/Level01_FrozenForest.unity");
            Folder(TileFolder); Folder(PaletteFolder);
            Undo.IncrementCurrentGroup(); undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Convert backgrounds to editable Tilemaps");
            var farMap = ConvertLayer(far, grid, true);
            var nearMap = ConvertLayer(near, grid, false);
            Require(oldWrapper.childCount == 0, "Background wrapper still contains objects.");
            Undo.DestroyObjectImmediate(oldWrapper.gameObject);
            farMap.transform.SetSiblingIndex(0);
            nearMap.transform.SetSiblingIndex(1);
            var farPalette = Palette(farMap, true);
            var nearPalette = Palette(nearMap, false);
            VerifyBrush(grid, farMap, farPalette);
            VerifyBrush(grid, nearMap, nearPalette);
            Require(ground.GetUsedTilesCount() == groundTiles && ground.transform.localToWorldMatrix == groundMatrix, "Ground changed during background conversion.");
            Require(ground.GetComponent<CompositeCollider2D>() != null && ground.GetComponent<TilemapCollider2D>() != null, "Ground colliders missing.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            results.AppendLine("PASS: Scene saved. Ground Tilemap and its colliders preserved. Backup: " + backup);
            File.WriteAllText(Report, results.ToString());
            OpenFarPalette();
        }
        catch (Exception error)
        {
            if (undoGroup >= 0) Undo.RevertAllDownToGroup(undoGroup);
            File.WriteAllText(Report, results + "ERROR: " + error);
            Debug.LogException(error);
        }
    }

    [MenuItem("Tools/Ooo Adventure/Map/Open Background Far Palette")]
    public static void OpenFarPalette() { OpenPalette(true); }

    [MenuItem("Tools/Ooo Adventure/Map/Open Background Near Palette")]
    public static void OpenNearPalette() { OpenPalette(false); }

    static void OpenPalette(bool far)
    {
        var palette = AssetDatabase.LoadAssetAtPath<GameObject>(PaletteFolder + (far ? "/FrozenForest_BackgroundFar.prefab" : "/FrozenForest_BackgroundNear.prefab"));
        var target = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None).FirstOrDefault(m => m.name == (far ? "BackgroundFarTilemap" : "BackgroundNearTilemap"));
        if (palette == null || target == null) return;
        Selection.activeGameObject = target.gameObject;
        EditorWindow.GetWindow<GridPaintPaletteWindow>("Tile Palette");
        GridPaintingState.palette = palette;
        GridPaintingState.scenePaintTarget = target.gameObject;
    }
}
