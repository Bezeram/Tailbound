using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class LevelInstantiator : MonoBehaviour
{
    // Comfortably below every other sortingOrder used in the project (all default to 0).
    private const int BackgroundSortingOrder = -100;

    [SerializeField] private GameObject _ScreenPrefab;
    [SerializeField] private GameObject _TilesPrefab;
    [SerializeField] private GameObject _PlayerPrefab;
    [SerializeField] private GameObject _CameraPrefab;
    [SerializeField] private string _LevelName;

    [Tooltip("Key that returns to the Level Editor scene while a playtest " +
             "launched from it is in progress (PlayTestSession.IsPlaytesting).")]
    [SerializeField] private KeyCode _ReturnToEditorKey = KeyCode.Escape;

    [Tooltip("Scene to load when returning to the editor - must match the " +
             "Level Editor scene's name in Build Settings.")]
    [SerializeField] private string _EditorSceneName = "LevelEditor";

    private void Awake()
    {
        // A playtest overrides the Inspector-assigned level - see PlayTestSession.
        string levelName = PlayTestSession.IsPlaytesting ? PlayTestSession.LevelSlotName : _LevelName;

        var level = LevelIO.Load(levelName);
        if (level == null)
        {
            Debug.LogError($"[LevelInstantiator] Could not load level '{levelName}'.");
            return;
        }

        Build(level);
    }

    private void Update()
    {
        // Only live during an editor-launched playtest.
        if (PlayTestSession.IsPlaytesting && Input.GetKeyDown(_ReturnToEditorKey))
            SceneManager.LoadScene(_EditorSceneName);
    }

    public void Build(LevelAsset level)
    {
        if (_ScreenPrefab == null)
        {
            Debug.LogError("[LevelInstantiator] ScreenPrefab is not assigned.");
            return;
        }

        if (_TilesPrefab == null)
        {
            Debug.LogError("[LevelInstantiator] TilesPrefab is not assigned.");
            return;
        }

        // Keyed by TileDef.Id + collidable, since collider setup differs per layer.
        var tileCache = new Dictionary<string, TileBase>();
        ScreenBox firstScreen = null;

        foreach (var screenDef in level.Screens)
        {
            var screenBox = BuildScreen(level, screenDef, tileCache);
            if (firstScreen == null)
                firstScreen = screenBox;
        }

        var levelManager = FindAnyObjectByType<LevelManager>();
        if (levelManager != null && firstScreen != null)
            levelManager.SetStartScreen(firstScreen);

        if (firstScreen != null)
            BuildPlayerAndCamera(firstScreen);

        // DeathBox needs the Player, which doesn't exist until after every
        // screen builds - re-run its init now that it does.
        foreach (var deathBox in FindObjectsByType<DeathBox>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            deathBox.RuntimeInit();
    }

    private ScreenBox BuildScreen(LevelAsset level, ScreenDef screenDef, Dictionary<string, TileBase> tileCache)
    {
        float cellSize = level.Grid.CellSize;

        var screenGO = Instantiate(_ScreenPrefab);
        screenGO.name = $"Screen_{screenDef.Id}";
        screenGO.transform.position = new Vector3(screenDef.Origin.x, screenDef.Origin.y, 0f) * cellSize;

        var screenBox = screenGO.GetComponent<ScreenBox>();
        screenBox.Size = (Vector2)screenDef.Size * cellSize;

        var content = screenGO.transform.Find("Content");

        // Clears the prefab's template content; DestroyImmediate so
        // RuntimeInit() below sees the final child list this frame.
        for (int i = content.childCount - 1; i >= 0; i--)
            DestroyImmediate(content.GetChild(i).gameObject);

        screenBox.TilesRoot = BuildTiles(level, screenDef, cellSize, tileCache);
        BuildEntities(content, level, screenDef, cellSize);

        // Awake() ran before Size/Content were finalized - re-run now that they're correct.
        screenBox.RuntimeInit();
        screenGO.transform.Find("DeathBox").GetComponent<DeathBox>().RuntimeInit();

        return screenBox;
    }

    // ------------------------------------------------------------------
    // Tiles
    // ------------------------------------------------------------------

    private GameObject BuildTiles(LevelAsset level, ScreenDef screenDef, float cellSize, Dictionary<string, TileBase> tileCache)
    {
        var gridGO = Instantiate(_TilesPrefab);
        gridGO.name = $"Tiles_Screen_{screenDef.Id}";
        gridGO.transform.position = new Vector3(screenDef.Origin.x, screenDef.Origin.y, 0f) * cellSize;
        
        var grid = gridGO.GetComponent<Grid>();
        if (grid != null)
            grid.cellSize = new Vector3(cellSize, cellSize, 1f);
        else
            Debug.LogError("[LevelInstantiator] TilesPrefab's root has no Grid component.");

        var backgroundTilemap = gridGO.transform.Find("Background")?.GetComponent<Tilemap>();
        var foregroundTilemap = gridGO.transform.Find("Foreground")?.GetComponent<Tilemap>();

        if (backgroundTilemap == null || foregroundTilemap == null)
        {
            Debug.LogError("[LevelInstantiator] TilesPrefab must have \"Background\" and \"Foreground\" children, each with a Tilemap component.");
            return gridGO;
        }
        
        // Set layer for actual collisions
        int solidGroundLayer = LayerMask.NameToLayer("SolidGround");
        foregroundTilemap.gameObject.layer = solidGroundLayer;

        // Background must render behind everything else - entities and
        // Foreground all default to sortingOrder 0, so without this the two
        // tilemaps' relative draw order isn't guaranteed.
        var backgroundRenderer = backgroundTilemap.GetComponent<TilemapRenderer>();
        if (backgroundRenderer != null)
        {
            backgroundRenderer.sortingOrder = BackgroundSortingOrder;
            backgroundRenderer.sortingLayerID = SortingLayer.NameToID("Background");
        }

        var foregroundRenderer = foregroundTilemap.GetComponent<TilemapRenderer>();
        if (foregroundRenderer != null)
            foregroundRenderer.sortingLayerID = SortingLayer.NameToID("Solid");

        // Composite avoids self-shadowing seams between adjacent solid tiles.
        foregroundTilemap.gameObject.AddComponent<ShadowCaster2D>();
        foregroundTilemap.gameObject.AddComponent<CompositeShadowCaster2D>();

        PaintTilemap(backgroundTilemap, level.Background, screenDef, tileCache, collidable: false);
        PaintTilemap(foregroundTilemap, level.Foreground, screenDef, tileCache, collidable: true);
        
        return gridGO;
    }

    private void PaintTilemap(
        Tilemap tilemap, TileLayer layer, ScreenDef screenDef, Dictionary<string, TileBase> tileCache, bool collidable)
    {
        // Only Foreground is collidable - every tile painted there generates collision.
        if (!layer.ScreenCells.TryGetValue(screenDef.Id, out var cells))
            return;

        foreach (var pair in cells)
        {
            if (!TileCatalog.Lookup.TryGetValue(pair.Value.TileId, out var def))
                continue;

            TileBase tile = GetOrCreateRuntimeTile(tileCache, def, collidable);
            tilemap.SetTile(new Vector3Int(pair.Key.x, pair.Key.y, 0), tile);
        }
    }

    private static TileBase GetOrCreateRuntimeTile(Dictionary<string, TileBase> cache, TileDef def, bool collidable)
    {
        string key = def.Id + (collidable ? "#fg" : "#bg");
        if (cache.TryGetValue(key, out var existing))
            return existing;

        TileBase tile;
        if (def.RuleTile != null)
        {
            tile = def.RuleTile;
        }
        else
        {
            var plainTile = ScriptableObject.CreateInstance<Tile>();
            plainTile.sprite = def.Sprite;
            plainTile.colliderType = collidable ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            tile = plainTile;
        }

        cache[key] = tile;
        return tile;
    }

    // ------------------------------------------------------------------
    // Entities
    // ------------------------------------------------------------------

    private void BuildEntities(Transform content, LevelAsset level, ScreenDef screenDef, float cellSize)
    {
        foreach (var instance in level.Entities)
        {
            if (instance.ScreenId == screenDef.Id)
                BuildEntity(content, instance, cellSize);
        }
    }

    private void BuildEntity(Transform content, EntityInstance instance, float cellSize)
    {
        if (!EntityCatalog.Lookup.TryGetValue(instance.TypeId, out var def))
        {
            Debug.LogWarning($"[LevelInstantiator] Unknown entity TypeId '{instance.TypeId}', skipping.");
            return;
        }

        GameObject root;
        switch (def.Backing)
        {
            case EntityBackingKind.NativePrefab:
                if (def.Prefab == null)
                {
                    Debug.LogWarning($"[LevelInstantiator] EntityDefinition '{def.TypeId}' has no Prefab assigned, skipping.");
                    return;
                }
                root = Instantiate(def.Prefab);
                break;

            case EntityBackingKind.ScriptBehavior:
                if (def.Script == null)
                {
                    Debug.LogWarning($"[LevelInstantiator] EntityDefinition '{def.TypeId}' has no Script assigned, skipping.");
                    return;
                }
                root = new GameObject(def.TypeId);
                break;

            default:
                Debug.LogWarning($"[LevelInstantiator] Unknown EntityBackingKind for '{def.TypeId}', skipping.");
                return;
        }

        // Parented/positioned before overrides, so Awake-time hierarchy/position reads are correct.
        root.transform.SetParent(content, false);
        root.name = $"Entity_{def.TypeId}_{instance.Id}";
        root.transform.localPosition = instance.LocalPosition * cellSize;
        root.transform.localRotation = Quaternion.Euler(0f, 0f, instance.Rotation);

        if (def.Backing == EntityBackingKind.NativePrefab)
        {
            // The prefab's own serialized fields are the default; overrides
            // apply directly on top.
            if (NativePrefabAdapterRegistry.TryGetForPrefab(def.Prefab, out var adapter))
            {
                instance.ComponentOverrides.TryGetValue(adapter.AdapterId, out var overrides);
                adapter.Apply(root, overrides ?? _EmptyProperties);
            }
        }
        else
        {
            root.AddComponent<ScriptEntityRunner>().Initialize(def.Script, instance);
        }
    }

    private static readonly Dictionary<string, PropertyValue> _EmptyProperties = new();

    // ------------------------------------------------------------------
    // Player / Camera
    // ------------------------------------------------------------------

    private void BuildPlayerAndCamera(ScreenBox startScreen)
    {
        var spawnPoint = startScreen.GetComponentInChildren<SpawnPoint>();
        if (spawnPoint == null)
        {
            Debug.LogWarning($"[LevelInstantiator] Start screen '{startScreen.name}' has no spawn point - Player/Camera not instantiated.");
            return;
        }

        GameObject player = null;
        if (_PlayerPrefab != null)
        {
            player = Instantiate(_PlayerPrefab, spawnPoint.transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("[LevelInstantiator] PlayerPrefab is not assigned - skipping Player.");
        }

        if (_CameraPrefab != null)
        {
            var cameraGO = Instantiate(_CameraPrefab);
            cameraGO.tag = "MainCamera";

            var camera = cameraGO.GetComponentInChildren<Camera>();
            if (camera != null && camera.orthographic)
            {
                float halfHeight = camera.orthographicSize;
                float halfWidth = halfHeight * camera.aspect;
                Vector3 bottomLeft = startScreen.BottomLeft;
                cameraGO.transform.position = new Vector3(
                    bottomLeft.x + halfWidth, bottomLeft.y + halfHeight, cameraGO.transform.position.z);
            }
            else
            {
                Debug.LogWarning("[LevelInstantiator] CameraPrefab has no orthographic Camera - couldn't align it to the screen.");
            }

            var cameraFollow = cameraGO.GetComponentInChildren<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.Screen = startScreen;
                if (player != null)
                    cameraFollow.Target = player.transform;
            }
        }
        else
        {
            Debug.LogWarning("[LevelInstantiator] CameraPrefab is not assigned - skipping Camera.");
        }
    }
}
