using System;
using System.Collections.Generic;
using UnityEngine;

public interface INativeComponentBinder
{
    Type UnityType { get; }
    List<PropertyDef> Schema { get; }

    void Apply(Component target, Dictionary<string, PropertyValue> properties);
}
