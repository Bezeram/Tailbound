using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pooled sprite-Image renderer for one TileLayer's painted cells within a
/// single screen. Same pool-and-toggle pattern as the grid lines in
/// ScreenCanvasView, split into a structural rebuild (SetCells, called only
/// when the painted set actually changes) and a cheap per-frame
/// reposition (Reposition, called every frame so tiles track pan/zoom).
/// </summary>
public class TileCellPool
{
    private static readonly Color MissingSpriteColor = new(1f, 0f, 1f, 0.5f);

    private readonly Transform _Parent;
    private readonly List<Image> _Pool = new();
    private readonly List<Vector2Int> _ActiveCells = new();

    public TileCellPool(Transform parent)
    {
        _Parent = parent;
    }

    public void SetCells(Dictionary<Vector2Int, TileRef> cells, Dictionary<string, TileDef> tileDefLookup)
    {
        _ActiveCells.Clear();

        if (cells != null)
        {
            EnsurePoolSize(cells.Count);

            int i = 0;
            foreach (var pair in cells)
            {
                Image image = _Pool[i];
                image.gameObject.SetActive(true);

                TileDef def = tileDefLookup.TryGetValue(pair.Value.TileId, out var found) ? found : null;
                image.sprite = def != null ? def.Sprite : null;
                image.color = def != null && def.Sprite != null ? Color.white : MissingSpriteColor;

                _ActiveCells.Add(pair.Key);
                i++;
            }
        }

        for (int i = _ActiveCells.Count; i < _Pool.Count; i++)
            _Pool[i].gameObject.SetActive(false);
    }

    public void Reposition(float cellPixelSize)
    {
        for (int i = 0; i < _ActiveCells.Count; i++)
        {
            var rect = (RectTransform)_Pool[i].transform;
            rect.anchoredPosition = (Vector2)_ActiveCells[i] * cellPixelSize;
            rect.sizeDelta = new Vector2(cellPixelSize, cellPixelSize);
        }
    }

    private void EnsurePoolSize(int count)
    {
        while (_Pool.Count < count)
        {
            var go = new GameObject("Tile", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_Parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;

            var image = go.GetComponent<Image>();
            // Tiles must never intercept clicks meant for the screen body -
            // both move/resize (Screens mode) and painting (Paint mode) rely
            // on the ScreenView's own root Image receiving the raycast.
            image.raycastTarget = false;

            _Pool.Add(image);
        }
    }
}
