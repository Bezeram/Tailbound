using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared logic for resolving a NativePrefab EntityInstance's adapter and
/// effective property values (instance override, else whatever the adapter
/// reads off the prefab asset itself) - used by both the entity inspector
/// (LevelEditorRuntimeController) and the canvas marker (EntityMarkerView),
/// so the two can't disagree on what "the current value" of a property is.
/// </summary>
public static class EntityPropertyResolver
{
    /// <summary>
    /// Resolves the EntityDefinition and matched adapter for a given
    /// EntityInstance in one call. False if the entity type is unknown,
    /// isn't NativePrefab-backed, has no Prefab assigned, or no adapter
    /// targets a component on that Prefab (see NativePrefabAdapterRegistry).
    /// </summary>
    public static bool TryResolveAdapter(EntityInstance instance, out EntityDefinition definition, out INativePrefabAdapter adapter)
    {
        adapter = null;

        if (!EntityCatalog.Lookup.TryGetValue(instance.TypeId, out definition))
            return false;

        if (definition.Backing != EntityBackingKind.NativePrefab || definition.Prefab == null)
            return false;

        return NativePrefabAdapterRegistry.TryGetForPrefab(definition.Prefab, out adapter);
    }

    public static PropertyValue GetEffectiveValue(EntityInstance instance, GameObject prefabAsset, INativePrefabAdapter adapter, PropertyDef propDef)
    {
        if (instance.ComponentOverrides.TryGetValue(adapter.AdapterId, out var overrides)
            && overrides.TryGetValue(propDef.Key, out var overrideValue))
            return overrideValue;

        return adapter.Read(prefabAsset, propDef.Key);
    }

    public static Dictionary<string, PropertyValue> GetEffectiveProperties(EntityInstance instance, GameObject prefabAsset, INativePrefabAdapter adapter)
    {
        var result = new Dictionary<string, PropertyValue>(adapter.Schema.Count);
        foreach (var propDef in adapter.Schema)
            result[propDef.Key] = GetEffectiveValue(instance, prefabAsset, adapter, propDef);
        return result;
    }
}
