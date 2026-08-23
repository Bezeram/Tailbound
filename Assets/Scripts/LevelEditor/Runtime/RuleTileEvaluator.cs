using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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

            // This screen owns the cell but has no tile there - definitely empty.
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
