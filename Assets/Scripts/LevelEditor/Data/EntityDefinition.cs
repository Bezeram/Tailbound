using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// A placeable entity type: the palette entry. Its editable property schema
/// is never authored here - it used to be a hand-picked List&lt;ComponentSpec&gt;
/// (removed; see git history and NativePrefabAdapterRegistry), which both
/// crashed Odin (its [ShowIf]'d Prefab field, see the odin-crash notes) and
/// couldn't actually express "wrap this prefab AND expose one of its
/// components" coherently. Now the schema is always derived automatically:
///   - NativePrefab: from whichever INativePrefabAdapter targets a component
///     already present on Prefab (see NativePrefabAdapterRegistry).
///   - ScriptBehavior: reserved for MiniScript-authored entities, which will
///     declare their own exposed variables in script - not implemented yet.
/// </summary>
[CreateAssetMenu(fileName = "EntityDefinition", menuName = "Level Editor/Entity Definition")]
public class EntityDefinition : SerializedScriptableObject
{
    [Tooltip("Stable key referenced by EntityInstance.TypeId.")]
    public string TypeId;
    public string DisplayName;
    public Sprite Icon;
    public string Category;

    // Grid snapping is a global editor mode (ScreenCanvasView.SnapToGridEnabled,
    // toggled in the toolbar, Ctrl inverts) rather than a per-type default -
    // no field here for it.

    [Tooltip("Marks this type as a spawn point for save-validation (every screen needs at least one) " +
             "and for LevelInstantiator, which places the Player/Camera at the start screen's spawn point.")]
    public bool IsSpawnPoint;

    // See TileDef.CollisionType for why this is EnumToggleButtons rather than a dropdown.
    [EnumToggleButtons]
    public EntityBackingKind Backing = EntityBackingKind.NativePrefab;

    [Tooltip("Only meaningful when Backing is NativePrefab - the hand-built prefab this entity " +
             "type instantiates wholesale. Always shown regardless of Backing rather than " +
             "conditionally with [ShowIf] - Odin's conditional-visibility drawers crash on this " +
             "Unity version (see the odin-crash project memory).")]
    public GameObject Prefab;
}
