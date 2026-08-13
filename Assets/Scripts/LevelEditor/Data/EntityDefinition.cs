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

    [Tooltip("Whether placing this entity type defaults to snapping to the grid.")]
    public bool DefaultGridSnap;

    public List<ComponentSpec> Components = new();
}
