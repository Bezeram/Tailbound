using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Picks the correct sprite for a TileDef.RuleTile by evaluating its rules
/// against neighboring cells - within the same screen, and also across into
/// whichever adjacent screen (if any) actually owns a neighbor cell that
/// falls outside this one's own bounds, so a rule tile paints correctly
/// right up to a shared screen edge instead of treating everything beyond
/// its own screen as empty. Works without a real Tilemap component - the
/// runtime editor's canvas renders painted cells as pooled uGUI Images
/// (TileCellPool), deliberately not an actual Tilemap (see LevelInstantiator's
/// history with that). Reads the RuleTile asset's own rule data directly
/// rather than going through Tilemap/ITilemap, which would need a real
/// Tilemap in the loop.
///
/// Scope: only literal "This"/"NotThis" neighbor matching, first matching
/// rule wins (same precedence a real Tilemap uses), and only a rule's first
/// sprite - no random/animated output, no RuleTransform rotation/mirror
/// variant matching. Covers ordinary terrain autotiling; a RuleTile that
/// relies on rotation/mirroring or random sprite variants won't render
/// identically to how it would inside a real Tilemap.
/// </summary>
public static class RuleTileEvaluator
{
    public static Sprite ResolveSprite(
        Vector2Int cell, TileDef def, Dictionary<Vector2Int, TileRef> cells, Dictionary<string, TileDef> lookup,
        ScreenDef screen, LevelAsset level, TileLayer layer)
    {
        if (def.RuleTile == null)
            return def.Sprite;

        foreach (var rule in def.RuleTile.m_TilingRules)
        {
            if (Matches(rule, cell, def.Id, cells, screen, level, layer))
                return FirstSprite(rule) ?? def.RuleTile.m_DefaultSprite;
        }

        return def.RuleTile.m_DefaultSprite;
    }

    private static bool Matches(
        RuleTile.TilingRule rule, Vector2Int cell, string selfTileId, Dictionary<Vector2Int, TileRef> cells,
        ScreenDef screen, LevelAsset level, TileLayer layer)
    {
        for (int i = 0; i < rule.m_Neighbors.Count; i++)
        {
            Vector3Int offset = rule.m_NeighborPositions[i];
            var neighborCell = new Vector2Int(cell.x + offset.x, cell.y + offset.y);

            bool isSameTile = TryGetNeighborTileId(neighborCell, cells, screen, level, layer, out string neighborTileId)
                && neighborTileId == selfTileId;

            int requirement = rule.m_Neighbors[i];
            if (requirement == RuleTile.TilingRuleOutput.Neighbor.This && !isSameTile)
                return false;
            if (requirement == RuleTile.TilingRuleOutput.Neighbor.NotThis && isSameTile)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Resolves one neighbor cell's TileId, whether it belongs to this
    /// screen or - if the local coordinate falls outside this screen's own
    /// bounds - to whichever other screen's bounds contain that world cell.
    /// A neighbor position that isn't owned by any screen at all (open space
    /// past a level's edge) is just empty, same as before.
    /// </summary>
    private static bool TryGetNeighborTileId(
        Vector2Int localCell, Dictionary<Vector2Int, TileRef> sameScreenCells,
        ScreenDef screen, LevelAsset level, TileLayer layer, out string tileId)
    {
        Vector2Int worldCell = screen.Origin + localCell;

        if (screen.Bounds.Contains(worldCell))
        {
            if (sameScreenCells != null && sameScreenCells.TryGetValue(localCell, out var sameRef))
            {
                tileId = sameRef.TileId;
                return true;
            }

            tileId = null;
            return false;
        }

        foreach (var otherScreen in level.Screens)
        {
            if (otherScreen.Id == screen.Id || !otherScreen.Bounds.Contains(worldCell))
                continue;

            Vector2Int otherLocal = worldCell - otherScreen.Origin;
            if (layer.ScreenCells.TryGetValue(otherScreen.Id, out var otherCells)
                && otherCells.TryGetValue(otherLocal, out var otherRef))
            {
                tileId = otherRef.TileId;
                return true;
            }

            // Found the screen that owns this world cell, but it has no
            // tile there - definitely empty, no need to keep searching.
            break;
        }

        tileId = null;
        return false;
    }

    private static Sprite FirstSprite(RuleTile.TilingRule rule)
    {
        return rule.m_Sprites != null && rule.m_Sprites.Length > 0 ? rule.m_Sprites[0] : null;
    }
}
