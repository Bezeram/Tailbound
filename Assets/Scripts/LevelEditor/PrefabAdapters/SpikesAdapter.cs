using System;
using System.Collections.Generic;
using UnityEngine;

public class SpikesAdapter : INativePrefabAdapter
{
    public Type TargetComponentType => typeof(Spikes);
    public string AdapterId => "Spikes";

    public List<PropertyDef> Schema { get; } = new()
    {
        new PropertyDef
        {
            Key = "Direction",
            Type = PropertyType.Int,
            DefaultValue = PropertyValue.FromInt((int)Spikes.SpikesDirection.Up),
            Label = "Direction (0=Up, 1=Left, 2=Down, 3=Right)",
        },
    };

    public PropertyValue Read(GameObject entityRoot, string propertyKey)
    {
        var spikes = entityRoot.GetComponent<Spikes>();
        int direction = spikes != null ? (int)spikes.Direction : (int)Spikes.SpikesDirection.Up;
        return PropertyValue.FromInt(direction);
    }

    public void Apply(GameObject entityRoot, Dictionary<string, PropertyValue> properties)
    {
        var spikes = entityRoot.GetComponent<Spikes>();
        if (spikes == null)
            return;

        if (properties.TryGetValue("Direction", out var directionProp) && directionProp.Type == PropertyType.Int)
            spikes.Direction = (Spikes.SpikesDirection)directionProp.IntValue;

        // Direction alone doesn't rotate the transform - ApplyDirection() does that.
        spikes.ApplyDirection();
    }

    public float GetEditorRotationDegrees(Dictionary<string, PropertyValue> properties)
    {
        var direction = properties.TryGetValue("Direction", out var directionProp) && directionProp.Type == PropertyType.Int
            ? (Spikes.SpikesDirection)directionProp.IntValue
            : Spikes.SpikesDirection.Up;

        return (int)direction * 90;
    }

    public Vector2 GetEditorScale(Dictionary<string, PropertyValue> properties) => Vector2.one;
}
