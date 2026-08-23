using System;
using System.Collections.Generic;
using UnityEngine;

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
