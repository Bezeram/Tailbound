using System;
using System.Collections.Generic;
using UnityEngine;

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

        // Direction alone doesn't rotate the transform - RuntimeInit() does that.
        spring.RuntimeInit();
    }

    public float GetEditorRotationDegrees(Dictionary<string, PropertyValue> properties)
    {
        var direction = properties.TryGetValue("Direction", out var directionProp) && directionProp.Type == PropertyType.Int
            ? (Spring.DirectionSpring)directionProp.IntValue
            : Spring.DirectionSpring.Up;

        return (int)direction * 90;
    }

    public Vector2 GetEditorScale(Dictionary<string, PropertyValue> properties) => Vector2.one;
}
