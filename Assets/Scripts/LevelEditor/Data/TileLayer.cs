using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A sparse tile layer (Background or Foreground), keyed first by owning
/// screen id and then by local cell coordinate within that screen. Screen
/// ownership is deliberate: moving a Screen's Origin moves every tile bound
/// to it without rewriting a single cell record.
/// </summary>
[Serializable]
public class TileLayer
{
    public Dictionary<int, Dictionary<Vector2Int, TileRef>> ScreenCells = new();

    public Dictionary<Vector2Int, TileRef> GetOrCreateScreenCells(int screenId)
    {
        if (!ScreenCells.TryGetValue(screenId, out var cells))
        {
            cells = new Dictionary<Vector2Int, TileRef>();
            ScreenCells[screenId] = cells;
        }

        return cells;
    }

    public void RemoveScreen(int screenId)
    {
        ScreenCells.Remove(screenId);
    }
}
