using System;
using UnityEngine;

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
