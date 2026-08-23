using System;
using UnityEngine;

[Serializable]
public class ScreenDef
{
    public int Id;

    [Tooltip("This screen's bottom-left corner, in grid cells.")]
    public Vector2Int Origin;

    [Tooltip("Size, in grid cells.")]
    public Vector2Int Size = new(16, 9);

    public RectInt Bounds => new(Origin, Size);
}
