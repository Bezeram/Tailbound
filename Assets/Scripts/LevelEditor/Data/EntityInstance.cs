using System;
using System.Collections.Generic;
using UnityEngine;

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

    public Dictionary<string, Dictionary<string, PropertyValue>> ComponentOverrides = new();
}
