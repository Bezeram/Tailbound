using System;
using UnityEngine;

/// <summary>
/// Declares one tunable property an adapter exposes for authoring - see
/// <see cref="INativePrefabAdapter"/> (one hand-built prefab's own script)
/// and <see cref="INativeComponentBinder"/> (a generic Unity component
/// type). Drives both the editor's inspector fields and which properties
/// the owning adapter's Apply/Read understands.
/// </summary>
[Serializable]
public class PropertyDef
{
    public string Key;
    public PropertyType Type;
    public PropertyValue DefaultValue;
    public string Label;
    [TextArea] public string Tooltip;

    public bool HasRange;
    public float MinValue;
    public float MaxValue;
}
