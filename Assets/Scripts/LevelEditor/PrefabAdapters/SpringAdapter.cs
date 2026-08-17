using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Exposes Spring.prefab's Direction as an editable property. Reference
/// implementation for INativePrefabAdapter - see NativePrefabAdapterRegistry.
/// Direction is stored as its underlying int (PropertyType has no enum kind,
/// by design - see PropertyType), so the label spells out what each value
/// means since Spring.DirectionSpring isn't a plain 0/1/2 sequence.
/// </summary>
public class SpringAdapter : INativePrefabAdapter
{
    public Type TargetComponentType => typeof(Spring);
    public string AdapterId => "Spring";

    public List<PropertyDef> Schema { get; } = new()
    {
        new PropertyDef
        {
            Key = "Direction",
            Type = PropertyType.Int,
            DefaultValue = PropertyValue.FromInt((int)Spring.DirectionSpring.Up),
            Label = "Direction (0=Up, 1=Left, -1=Right)",
        },
    };

    public PropertyValue Read(GameObject entityRoot, string propertyKey)
    {
        var spring = entityRoot.GetComponent<Spring>();
        int direction = spring != null ? (int)spring.Direction : (int)Spring.DirectionSpring.Up;
        return PropertyValue.FromInt(direction);
    }

    public void Apply(GameObject entityRoot, Dictionary<string, PropertyValue> properties)
    {
        var spring = entityRoot.GetComponent<Spring>();
        if (spring == null)
            return;

        if (properties.TryGetValue("Direction", out var directionProp) && directionProp.Type == PropertyType.Int)
            spring.Direction = (Spring.DirectionSpring)directionProp.IntValue;

        // Direction alone doesn't rotate the transform - RuntimeInit() does
        // (also re-caches Animator/AudioSource, harmless here).
        spring.RuntimeInit();
    }
}
