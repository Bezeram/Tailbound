using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Starter native-component adapter for BoxCollider2D.
/// </summary>
public class BoxCollider2DBinder : INativeComponentBinder
{
    public Type UnityType => typeof(BoxCollider2D);

    public List<PropertyDef> Schema { get; } = new()
    {
        new PropertyDef { Key = "Size", Type = PropertyType.Vector2, DefaultValue = PropertyValue.FromVector2(Vector2.one), Label = "Size" },
        new PropertyDef { Key = "Offset", Type = PropertyType.Vector2, Label = "Offset" },
        new PropertyDef { Key = "IsTrigger", Type = PropertyType.Bool, Label = "Is Trigger" },
    };

    public void Apply(Component target, Dictionary<string, PropertyValue> properties)
    {
        if (target is not BoxCollider2D collider)
            return;

        if (properties.TryGetValue("Size", out var sizeProp) && sizeProp.Type == PropertyType.Vector2)
            collider.size = sizeProp.Vector2Value;

        if (properties.TryGetValue("Offset", out var offsetProp) && offsetProp.Type == PropertyType.Vector2)
            collider.offset = offsetProp.Vector2Value;

        if (properties.TryGetValue("IsTrigger", out var triggerProp) && triggerProp.Type == PropertyType.Bool)
            collider.isTrigger = triggerProp.BoolValue;
    }
}
