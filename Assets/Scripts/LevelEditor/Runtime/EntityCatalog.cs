using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Session-wide catalog of every EntityDefinition under a Resources folder,
/// keyed by TypeId. Same shape as TileCatalog - loaded once, lazily, shared
/// by the entity palette panel and every ScreenView's marker rendering.
/// </summary>
public static class EntityCatalog
{
    private static Dictionary<string, EntityDefinition> _Lookup;
    private static List<EntityDefinition> _All;

    public static Dictionary<string, EntityDefinition> Lookup
    {
        get { EnsureLoaded(); return _Lookup; }
    }

    public static List<EntityDefinition> All
    {
        get { EnsureLoaded(); return _All; }
    }

    private static void EnsureLoaded()
    {
        if (_Lookup != null)
            return;

        _Lookup = new Dictionary<string, EntityDefinition>();
        _All = new List<EntityDefinition>();

        foreach (var def in Resources.LoadAll<EntityDefinition>(""))
        {
            if (string.IsNullOrEmpty(def.TypeId))
                continue;

            _Lookup[def.TypeId] = def;
            _All.Add(def);
        }
    }
}
