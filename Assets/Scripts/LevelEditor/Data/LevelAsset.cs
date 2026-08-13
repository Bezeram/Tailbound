using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The canonical, source-of-truth description of a level. Authored and
/// edited entirely at runtime by the level editor UI, then written to disk
/// as JSON via LevelIO - there is no Unity scene or ScriptableObject asset
/// backing this data.
/// </summary>
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

    public int NewScreenId()
    {
        _LastScreenId++;
        return _LastScreenId;
    }

    public ScreenDef GetScreen(int id)
    {
        return Screens.Find(s => s.Id == id);
    }

    /// <summary>
    /// Whether candidate would overlap any screen other than excludeId.
    /// Screens that merely touch at an edge are not considered overlapping.
    /// </summary>
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

    /// <summary>
    /// Removes a screen and every tile/entity owned by it, so nothing is
    /// left referencing a ScreenId that no longer exists.
    /// </summary>
    public void RemoveScreen(int screenId)
    {
        Screens.RemoveAll(s => s.Id == screenId);
        Background.RemoveScreen(screenId);
        Foreground.RemoveScreen(screenId);
        Entities.RemoveAll(e => e.ScreenId == screenId);
    }
}
