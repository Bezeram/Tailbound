using System.Collections.Generic;
using UnityEngine;

public static class TileCatalog
{
    private static Dictionary<string, TileDef> _Lookup;
    private static List<TileDef> _All;

    public static Dictionary<string, TileDef> Lookup
    {
        get { EnsureLoaded(); return _Lookup; }
    }

    public static List<TileDef> All
    {
        get { EnsureLoaded(); return _All; }
    }

    private static void EnsureLoaded()
    {
        if (_Lookup != null)
            return;

        _Lookup = new Dictionary<string, TileDef>();
        _All = new List<TileDef>();

        foreach (var tileset in Resources.LoadAll<Tileset>(""))
        {
            foreach (var tile in tileset.Tiles)
            {
                if (string.IsNullOrEmpty(tile.Id))
                    continue;

                _Lookup[tile.Id] = tile;
                _All.Add(tile);
            }
        }
    }
}
