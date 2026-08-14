using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Picks the correct sprite for a TileDef.RuleTile by evaluating its rules
/// against neighboring cells within the same screen/layer, without needing
/// a real Tilemap component - the runtime editor's canvas renders painted
/// cells as pooled uGUI Images (TileCellPool), deliberately not an actual
/// Tilemap (see LevelInstantiator's history with that). Reads the RuleTile
/// asset's own rule data directly rather than going through
/// Tilemap/ITilemap, which would need a real Tilemap in the loop.
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
    public static Sprite ResolveSprite(Vector2Int cell, TileDef def, Dictionary<Vector2Int, TileRef> cells, Dictionary<string, TileDef> lookup)
    {
        if (def.RuleTile == null)
            return def.Sprite;

        foreach (var rule in def.RuleTile.m_TilingRules)
        {
            if (Matches(rule, cell, def.Id, cells))
                return FirstSprite(rule) ?? def.RuleTile.m_DefaultSprite;
        }

        return def.RuleTile.m_DefaultSprite;
    }

    private static bool Matches(RuleTile.TilingRule rule, Vector2Int cell, string selfTileId, Dictionary<Vector2Int, TileRef> cells)
    {
        for (int i = 0; i < rule.m_Neighbors.Count; i++)
        {
            Vector3Int offset = rule.m_NeighborPositions[i];
            var neighborCell = new Vector2Int(cell.x + offset.x, cell.y + offset.y);

            bool isSameTile = cells.TryGetValue(neighborCell, out var neighborRef) && neighborRef.TileId == selfTileId;

            int requirement = rule.m_Neighbors[i];
            if (requirement == RuleTile.TilingRuleOutput.Neighbor.This && !isSameTile)
                return false;
            if (requirement == RuleTile.TilingRuleOutput.Neighbor.NotThis && isSameTile)
                return false;
        }

        return true;
    }

    private static Sprite FirstSprite(RuleTile.TilingRule rule)
    {
        return rule.m_Sprites != null && rule.m_Sprites.Length > 0 ? rule.m_Sprites[0] : null;
    }
}
