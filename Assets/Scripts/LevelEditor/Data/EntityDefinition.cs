using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityDefinition", menuName = "Level Editor/Entity Definition")]
public class EntityDefinition : SerializedScriptableObject
{
    [Tooltip("Stable key referenced by EntityInstance.TypeId.")]
    public string TypeId;
    public string DisplayName;
    public Sprite Icon;
    public string Category;

    // Grid snapping is a global editor mode, not a per-type default - no field here for it.

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

    [Tooltip("Only meaningful when Backing is ScriptBehavior - the MiniScript source this entity " +
             "type runs (a plain .ms text file, imported as a TextAsset). Always shown regardless " +
             "of Backing, same reasoning as Prefab above.")]
    public TextAsset Script;
}
