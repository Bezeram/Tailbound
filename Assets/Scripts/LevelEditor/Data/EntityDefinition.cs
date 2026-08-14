using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// A placeable entity type: the palette entry. Its Components list is the
/// default composition every EntityInstance of this TypeId starts from;
/// instances layer their own ComponentOverrides on top.
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

    public List<ComponentSpec> Components = new();
}
