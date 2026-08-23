using System.Collections.Generic;
using UnityEngine;

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

        RuntimeEntityIO.LoadAll(def => AddRuntimeDefinition(def));
    }

    public static void Invalidate()
    {
        _Lookup = null;
        _All = null;
    }

    public static void AddRuntimeDefinition(EntityDefinition def)
    {
        EnsureLoaded();

        if (def == null || string.IsNullOrEmpty(def.TypeId))
            return;

        _Lookup[def.TypeId] = def;
        if (!_All.Contains(def))
            _All.Add(def);
    }
}
