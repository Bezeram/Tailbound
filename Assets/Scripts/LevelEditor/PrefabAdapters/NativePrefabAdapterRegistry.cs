using System.Collections.Generic;
using UnityEngine;

public static class NativePrefabAdapterRegistry
{
    private static readonly List<INativePrefabAdapter> _Adapters = new()
    {
        new SpringAdapter(),
    };

    public static bool TryGetForPrefab(GameObject prefab, out INativePrefabAdapter adapter)
    {
        adapter = null;
        if (prefab == null)
            return false;

        foreach (var candidate in _Adapters)
        {
            if (prefab.GetComponent(candidate.TargetComponentType) != null)
            {
                adapter = candidate;
                return true;
            }
        }

        return false;
    }
}
