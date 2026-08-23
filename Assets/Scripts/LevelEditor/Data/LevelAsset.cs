using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelAsset
{
    public GridConfig Grid = new();
    public List<ScreenDef> Screens = new();

    public TileLayer Background = new();
    public TileLayer Foreground = new();

    public List<EntityInstance> Entities = new();
    public List<Tileset> UsedTilesets = new();

    [SerializeField] private int _LastScreenId = -1;
    [SerializeField] private int _LastEntityId = -1;

    public int NewScreenId()
    {
        _LastScreenId++;
        return _LastScreenId;
    }

    public int NewEntityId()
    {
        _LastEntityId++;
        return _LastEntityId;
    }

    public ScreenDef GetScreen(int id)
    {
        return Screens.Find(s => s.Id == id);
    }

    public bool ScreenOverlaps(RectInt candidate, int excludeId)
    {
        foreach (var screen in Screens)
        {
            if (screen.Id == excludeId)
                continue;

            if (candidate.Overlaps(screen.Bounds))
                return true;
        }

        return false;
    }

    public void RemoveScreen(int screenId)
    {
        Screens.RemoveAll(s => s.Id == screenId);
        Background.RemoveScreen(screenId);
        Foreground.RemoveScreen(screenId);
        Entities.RemoveAll(e => e.ScreenId == screenId);
    }
}
