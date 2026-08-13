using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One placed entity. Position is always a free Vector2 relative to its
/// owning screen's Origin - SnappedToGrid is only a placement-time UX
/// convenience, never a storage constraint.
/// </summary>
[Serializable]
public class EntityInstance
{
    public int Id;
    public int ScreenId;

    [Tooltip("References an EntityDefinition.TypeId.")]
    public string TypeId;

    [Tooltip("Position relative to the owning screen's Origin.")]
    public Vector2 LocalPosition;
    public float Rotation;
    public bool SnappedToGrid;

    /// <summary>
    /// Per-instance property overrides, keyed by the owning component's
    /// ComponentTypeId, layered on top of the EntityDefinition's defaults.
    /// </summary>
    public Dictionary<string, Dictionary<string, PropertyValue>> ComponentOverrides = new();
}
