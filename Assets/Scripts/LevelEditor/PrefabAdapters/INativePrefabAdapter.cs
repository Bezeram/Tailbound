using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A curated adapter exposing part of one specific hand-built prefab's own
/// script (e.g. Spring, Zipline) as editable properties in the runtime
/// editor - the NativePrefab counterpart to INativeComponentBinder, which
/// adapts a generic Unity component type instead of one particular prefab's
/// own gameplay script. Deliberately not generic reflection over arbitrary
/// fields, same reasoning as INativeComponentBinder: the surface a level
/// designer (or eventually a MiniScript-composed entity) can touch should be
/// an intentional, stable choice, not whatever a script happens to expose.
/// </summary>
public interface INativePrefabAdapter
{
    /// <summary>
    /// The prefab's own distinguishing script component, e.g. typeof(Spring).
    /// NativePrefabAdapterRegistry matches an adapter to an EntityDefinition
    /// by checking whether its Prefab carries this component - not by
    /// TypeId or prefab identity - so renaming an EntityDefinition's TypeId,
    /// or re-pointing it at a differently-named duplicate of the same
    /// prefab, doesn't silently lose its adapter.
    /// </summary>
    Type TargetComponentType { get; }

    /// <summary>Stable key this adapter's properties are stored under in
    /// EntityInstance.ComponentOverrides.</summary>
    string AdapterId { get; }

    List<PropertyDef> Schema { get; }

    /// <summary>
    /// Reads a single property's current value directly off entityRoot -
    /// works equally on the EntityDefinition's own Prefab asset (to show a
    /// not-yet-overridden default in the inspector) or on an instantiated
    /// instance, since both carry the same component with the same fields.
    /// </summary>
    PropertyValue Read(GameObject entityRoot, string propertyKey);

    /// <summary>Applies instance overrides (already merged over nothing else -
    /// unlike INativeComponentBinder, there's no separate EntityDefinition-
    /// level default to merge under, since the prefab asset's own serialized
    /// values already are the default) onto the given instantiated entity.</summary>
    void Apply(GameObject entityRoot, Dictionary<string, PropertyValue> properties);

    /// <summary>
    /// Editor-only visual rotation (degrees around Z) derived from the given
    /// effective property values (see EntityPropertyResolver), applied to
    /// this entity's marker on the runtime editor's canvas so a rotation-
    /// affecting property (e.g. Spring's Direction) previews correctly while
    /// placing/editing - should mirror whatever Apply() does to the real
    /// instantiated GameObject's transform. Return 0 for adapters with
    /// nothing that should rotate the marker.
    /// </summary>
    float GetEditorRotationDegrees(Dictionary<string, PropertyValue> properties);

    /// <summary>Editor-only visual scale multiplier, same idea as
    /// GetEditorRotationDegrees. Return Vector2.one for adapters with
    /// nothing that should scale the marker.</summary>
    Vector2 GetEditorScale(Dictionary<string, PropertyValue> properties);
}
