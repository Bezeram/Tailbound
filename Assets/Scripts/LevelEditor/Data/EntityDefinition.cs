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

    public List<ComponentSpec> Components = new();
}
