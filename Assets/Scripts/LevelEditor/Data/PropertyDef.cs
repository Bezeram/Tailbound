using System;
using UnityEngine;

/// <summary>
/// Declares one tunable property that a <see cref="ComponentSpec"/> or an
/// <see cref="EntityDefinition"/> exposes for authoring. Drives both the
/// editor's inspector fields and (for native adapters) which properties an
/// <see cref="INativeComponentBinder"/> understands.
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
