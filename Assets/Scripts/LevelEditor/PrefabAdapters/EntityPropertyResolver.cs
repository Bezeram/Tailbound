using System.Collections.Generic;
using UnityEngine;

public static class EntityPropertyResolver
{
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
