using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds real gameplay GameObjects (ScreenBox/DeathBox, Tilemaps, entities,
/// Player, Camera) from a LevelAsset, reusing the existing runtime systems
/// (ScreenBox, LevelManager, SpawnPoint, CameraFollow) rather than
/// reimplementing them - LevelManager's own Start() already discovers
/// screens/spawn points/bananas via FindObjectsByType, so this only needs to
/// make sure they exist in the scene before that runs. All work happens in
/// Awake(), since Unity guarantees every Awake() completes before any
/// Start() does, which is what lets SetStartScreen (and the Player/Camera
/// this creates) be in place before LevelManager.Start() reads them.
/// </summary>
public class LevelInstantiator : MonoBehaviour
{
    [SerializeField] private GameObject _ScreenPrefab;
    [SerializeField] private GameObject _TilesPrefab;
    [SerializeField] private GameObject _PlayerPrefab;
    [SerializeField] private GameObject _CameraPrefab;
    [SerializeField] private string _LevelName;

    private void Awake()
    {
        var level = LevelIO.Load(_LevelName);
        if (level == null)
        {
            Debug.LogError($"[LevelInstantiator] Could not load level '{_LevelName}'.");
            return;
        }

        Build(level);
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

        // Keyed by TileDef.Id + whether it's on the collidable layer, since
        // the same tile could paint into either layer with a different
        // resulting collider setup.
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

        // Remove the prefab's baked-in template content (its default Spawn
        // instance) - it's sized/positioned for the template's default area,
        // not this screen. Real content (below) replaces it. DestroyImmediate
        // because RuntimeInit(), later in this method, needs the final child
        // list synchronously - a deferred Destroy() would still be found by
        // GetComponentsInChildren<SpawnPoint>() this same frame.
        for (int i = content.childCount - 1; i >= 0; i--)
            DestroyImmediate(content.GetChild(i).gameObject);

        screenBox.TilesRoot = BuildTiles(level, screenDef, cellSize, tileCache);
        BuildEntities(content, level, screenDef, cellSize);

        // Awake() already ran once for both of these, synchronously during
        // Instantiate() above - before Size/Content were finalized. Re-run
        // now that they're correct.
        screenBox.RuntimeInit();
        screenGO.transform.Find("DeathBox").GetComponent<DeathBox>().RuntimeInit();

        return screenBox;
    }

    // ------------------------------------------------------------------
    // Tiles
    // ------------------------------------------------------------------

    /// <summary>
    /// Instantiates _TilesPrefab (Grid root with pre-authored "Background"/
    /// "Foreground" Tilemap children, Foreground already carrying its
    /// TilemapCollider2D/CompositeCollider2D/Rigidbody2D) as a scene-root
    /// sibling of the Screen, never a child of it - confirmed by testing
    /// that Grid/Tilemap GameObjects nested under Screen never get a
    /// working Grid, regardless of prefab vs. procedural construction or
    /// construction order. Since it's no longer parented under the screen,
    /// its world position has to be set explicitly to match the screen's
    /// origin instead of inheriting it.
    /// </summary>
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

        PaintTilemap(backgroundTilemap, level.Background, screenDef, tileCache, collidable: false);
        PaintTilemap(foregroundTilemap, level.Foreground, screenDef, tileCache, collidable: true);
        
        return gridGO;
    }

    private void PaintTilemap(
        Tilemap tilemap, TileLayer layer, ScreenDef screenDef, Dictionary<string, TileBase> tileCache, bool collidable)
    {
        // Only Foreground is collidable, via whatever collider setup is
        // already on the prefab's Foreground child - and only its
        // Solid-collision-type tiles actually generate collision, via their
        // own colliderType (set in GetOrCreateRuntimeTile). OneWayPlatform/
        // Hazard/Ladder render but have no collision or special physics
        // behavior yet - a scoped-out follow-up.
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

    /// <summary>
    /// A RuleTile is used directly as the tile object - a real Tilemap
    /// evaluates its neighbor rules natively and continuously (including
    /// re-evaluating as surrounding tiles change), so there's nothing to
    /// resolve here ourselves. This previously always built a plain Tile
    /// from TileDef.Sprite regardless, which is null for TileDefs authored
    /// with only a RuleTile (a normal way to set one up) - genuinely
    /// invisible tiles, unrelated to the Grid-hierarchy question.
    /// </summary>
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
            plainTile.colliderType = collidable && def.CollisionType == TileCollisionType.Solid
                ? Tile.ColliderType.Grid
                : Tile.ColliderType.None;
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

        // A NativePrefab entity is one opaque, hand-built prefab (Zipline,
        // Spring, ...) - it's instantiated wholesale as the entity root,
        // never mixed with other component specs.
        bool isPrefabEntity = def.Components.Count == 1 && def.Components[0].Kind == ComponentKind.NativePrefab;

        GameObject root;
        if (isPrefabEntity)
        {
            var spec = def.Components[0];
            if (spec.Prefab == null)
            {
                Debug.LogWarning($"[LevelInstantiator] EntityDefinition '{def.TypeId}' has no Prefab assigned, skipping.");
                return;
            }

            root = Instantiate(spec.Prefab);
        }
        else
        {
            root = new GameObject(def.TypeId);
        }

        // Parented (and positioned) before adding components, same reasoning
        // as the Tilemap ordering above - keeps any future binder that reads
        // the hierarchy or world position at Awake-time correct too.
        root.transform.SetParent(content, false);
        root.name = $"Entity_{def.TypeId}_{instance.Id}";
        root.transform.localPosition = instance.LocalPosition * cellSize;
        root.transform.localRotation = Quaternion.Euler(0f, 0f, instance.Rotation);

        if (!isPrefabEntity)
        {
            foreach (var spec in def.Components)
            {
                if (spec.Kind == ComponentKind.ScriptBehavior)
                {
                    Debug.LogWarning($"[LevelInstantiator] ScriptBehavior isn't supported yet (entity '{def.TypeId}'), skipping component.");
                    continue;
                }

                if (spec.Kind != ComponentKind.NativeUnityComponent)
                    continue;

                if (!NativeComponentBinderRegistry.TryGet(spec.ComponentTypeId, out var binder))
                {
                    Debug.LogWarning($"[LevelInstantiator] No binder registered for '{spec.ComponentTypeId}' (entity '{def.TypeId}'), skipping component.");
                    continue;
                }

                var component = root.AddComponent(binder.UnityType);
                binder.Apply(component, MergeProperties(spec, instance));
            }

            // Only the implicit Transform means every component spec above
            // failed to resolve - the entity exists (as reported) but has
            // nothing to render or collide with. Most likely cause: the
            // EntityDefinition's Components don't reference a registered
            // binder id, or the binder's properties (e.g. SpriteRenderer's
            // "Sprite" path) were never actually configured.
            if (root.GetComponents<Component>().Length <= 1)
            {
                Debug.LogWarning(
                    $"[LevelInstantiator] Entity '{def.TypeId}' (instance {instance.Id}) has no components after " +
                    "resolution - check its EntityDefinition.Components reference a registered binder id " +
                    "(see NativeComponentBinderRegistry) with properties actually set.");
            }
        }
    }

    private static Dictionary<string, PropertyValue> MergeProperties(ComponentSpec spec, EntityInstance instance)
    {
        var merged = new Dictionary<string, PropertyValue>(spec.Properties);

        if (instance.ComponentOverrides.TryGetValue(spec.ComponentTypeId, out var overrides))
        {
            foreach (var pair in overrides)
                merged[pair.Key] = pair.Value;
        }

        return merged;
    }

    // ------------------------------------------------------------------
    // Player / Camera
    // ------------------------------------------------------------------

    /// <summary>
    /// Spawns the Player at the start screen's spawn point and the Camera
    /// aligned bottom-left-to-bottom-left with that same screen. Skips
    /// entirely (no Player, no Camera) if the screen has no spawn point -
    /// there'd be nowhere sensible to put either.
    /// </summary>
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
