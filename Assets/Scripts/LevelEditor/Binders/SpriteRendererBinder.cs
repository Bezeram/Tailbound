using System;
using System.Collections.Generic;
using UnityEngine;

public class SpriteRendererBinder : INativeComponentBinder
{
    public Type UnityType => typeof(SpriteRenderer);

    public List<PropertyDef> Schema { get; } = new()
    {
        new PropertyDef { Key = "Sprite", Type = PropertyType.String, Label = "Sprite (Resources path)" },
        new PropertyDef { Key = "Color", Type = PropertyType.Color, DefaultValue = PropertyValue.FromColor(Color.white), Label = "Color" },
        new PropertyDef { Key = "SortingOrder", Type = PropertyType.Int, Label = "Sorting Order" },
    };

    public void Apply(Component target, Dictionary<string, PropertyValue> properties)
    {
        if (target is not SpriteRenderer renderer)
            return;

        if (properties.TryGetValue("Sprite", out var spriteProp)
            && spriteProp.Type == PropertyType.String
            && !string.IsNullOrEmpty(spriteProp.StringValue))
        {
            renderer.sprite = RuntimeResourceLoader.LoadSprite(spriteProp.StringValue);
        }

        if (properties.TryGetValue("Color", out var colorProp) && colorProp.Type == PropertyType.Color)
            renderer.color = colorProp.ColorValue;

        if (properties.TryGetValue("SortingOrder", out var sortProp) && sortProp.Type == PropertyType.Int)
            renderer.sortingOrder = sortProp.IntValue;
    }
}
