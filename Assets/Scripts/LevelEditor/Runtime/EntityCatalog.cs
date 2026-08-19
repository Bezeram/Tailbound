using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Session-wide catalog of every EntityDefinition, keyed by TypeId - both
/// the ones built into the project under a Resources folder (Same shape as
/// TileCatalog) and any modder-created ones saved on disk via
/// RuntimeEntityIO, so a level built entirely in a standalone build (no
/// Unity Editor) can still define new entity types. Loaded once, lazily,
/// shared by the entity palette panel and every ScreenView's marker rendering.
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

        RuntimeEntityIO.LoadAll(def => AddRuntimeDefinition(def));
    }

    /// <summary>Forces the next Lookup/All access to re-scan Resources and
    /// re-load every RuntimeEntityIO entity from disk. Rarely needed -
    /// AddRuntimeDefinition is the normal way a freshly-created entity shows
    /// up without waiting for this.</summary>
    public static void Invalidate()
    {
        _Lookup = null;
        _All = null;
    }

    /// <summary>Registers one entity definition immediately, without a full
    /// reload - called right after RuntimeEntityIO.Create so a newly-created
    /// entity type shows up in the palette right away.</summary>
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
