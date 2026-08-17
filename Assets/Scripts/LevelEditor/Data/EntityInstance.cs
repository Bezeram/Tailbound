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
    /// Per-instance property overrides, keyed by the owning adapter's
    /// AdapterId (INativePrefabAdapter.AdapterId for a NativePrefab entity -
    /// see NativePrefabAdapterRegistry), layered on top of whatever value
    /// the adapter reads directly off the entity's own prefab/instance as
    /// the default. Only ever has one top-level key today (one adapter per
    /// NativePrefab entity), but stays a nested dictionary since a future
    /// MiniScript entity may expose properties from several adapters at once.
    /// </summary>
    public Dictionary<string, Dictionary<string, PropertyValue>> ComponentOverrides = new();
}
