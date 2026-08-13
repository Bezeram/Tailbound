using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A curated, 1:1 adapter over a real Unity component type. Deliberately
/// not generic reflection over arbitrary fields - each binder whitelists
/// exactly the properties it exposes, so the surface a MiniScript-composed
/// entity (or the editor's inspector) can touch is intentional and stable.
/// </summary>
public interface INativeComponentBinder
{
    Type UnityType { get; }
    List<PropertyDef> Schema { get; }

    void Apply(Component target, Dictionary<string, PropertyValue> properties);
}
