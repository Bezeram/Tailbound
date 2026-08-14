using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// One component making up an <see cref="EntityDefinition"/>'s default
/// composition. For NativePrefab, <see cref="Prefab"/> is instantiated
/// directly as the whole entity (ComponentTypeId is just a label there).
/// For NativeUnityComponent, ComponentTypeId is a registered binder id (e.g.
/// "SpriteRenderer"). ScriptBehavior (ComponentTypeId as a script asset
/// path) is reserved for future MiniScript-authored entities.
/// </summary>
[Serializable]
public class ComponentSpec
{
    // See TileDef.CollisionType for why this is EnumToggleButtons rather than a dropdown.
    [EnumToggleButtons]
    public ComponentKind Kind;
    public string ComponentTypeId;
    public Dictionary<string, PropertyValue> Properties = new();

    [ShowIf("Kind", ComponentKind.NativePrefab)]
    public GameObject Prefab;
}
