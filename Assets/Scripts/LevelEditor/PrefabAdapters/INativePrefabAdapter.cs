using System;
using System.Collections.Generic;
using UnityEngine;

public interface INativePrefabAdapter
{
    Type TargetComponentType { get; }

    string AdapterId { get; }

    List<PropertyDef> Schema { get; }

    PropertyValue Read(GameObject entityRoot, string propertyKey);

    void Apply(GameObject entityRoot, Dictionary<string, PropertyValue> properties);

    float GetEditorRotationDegrees(Dictionary<string, PropertyValue> properties);

    Vector2 GetEditorScale(Dictionary<string, PropertyValue> properties);
}
