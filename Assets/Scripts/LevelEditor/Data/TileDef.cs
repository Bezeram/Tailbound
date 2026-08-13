using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// One paintable tile within a <see cref="Tileset"/>. Serves both the
/// Background and Foreground layers - <see cref="CollisionType"/> is simply
/// unused when painted on the Background.
/// </summary>
[Serializable]
public class TileDef
{
    [Tooltip("Stable key referenced by TileRef. Keep unchanged once tiles have been painted with it.")]
    public string Id;
    public string DisplayName;

    [Tooltip("Use either a plain Sprite for simple/decorative tiles, or a RuleTile for autotiled terrain.")]
    public Sprite Sprite;
    public RuleTile RuleTile;

    // EnumToggleButtons instead of a dropdown: Odin's dropdown-selector popup
    // (OdinMenuTree) crashes on this Unity version with a MissingMethodException
    // from a removed/changed internal UIElements API. Toggle buttons use a
    // different, simpler drawer that avoids that code path entirely.
    [EnumToggleButtons]
    public TileCollisionType CollisionType = TileCollisionType.None;
    public string Category;
}
