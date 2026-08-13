using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// One component making up an <see cref="EntityDefinition"/>'s default
/// composition. <see cref="ComponentTypeId"/> means different things
/// depending on <see cref="Kind"/>: a prefab name for NativePrefab, a
/// registered binder id (e.g. "SpriteRenderer") for NativeUnityComponent,
/// or a script asset path for ScriptBehavior.
/// </summary>
[Serializable]
public class ComponentSpec
{
    // See TileDef.CollisionType for why this is EnumToggleButtons rather than a dropdown.
    [EnumToggleButtons]
    public ComponentKind Kind;
    public string ComponentTypeId;
    public Dictionary<string, PropertyValue> Properties = new();
}
