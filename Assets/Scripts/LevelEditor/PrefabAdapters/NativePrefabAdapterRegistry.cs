using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Looks up the INativePrefabAdapter (if any) for an EntityDefinition's
/// Prefab, by checking which registered adapter's TargetComponentType the
/// prefab actually carries. Manually registered rather than reflection-
/// scanned, on purpose - same reasoning as NativeComponentBinderRegistry:
/// which prefabs expose editable properties should be a deliberate choice.
/// Add one entry here per hand-built prefab that should be editable from the
/// runtime editor's entity inspector.
/// </summary>
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
