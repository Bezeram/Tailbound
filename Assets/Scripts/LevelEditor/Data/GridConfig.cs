using System;

/// <summary>
/// The single grid every Screen, tile, and (optionally) entity in a level
/// binds to. World units per cell.
/// </summary>
[Serializable]
public class GridConfig
{
    public float CellSize = 1f;
}
