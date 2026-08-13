using System;
using UnityEngine;

/// <summary>
/// A room within a level: a rectangle on the shared grid. Tiles and entities
/// bind to a screen by <see cref="Id"/> and store coordinates relative to
/// <see cref="Origin"/>, so moving/resizing a screen moves its content with
/// it without touching any of that content's records.
/// </summary>
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
