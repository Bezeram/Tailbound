using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class LayerHighlight2D : MonoBehaviour
{
    [Header("What to highlight")]
    public LayerMask targetLayers;              // Select the layer(s) to highlight
    public bool includeInactive = false;        // Include inactive children when sizing
    public bool autoRefresh = true;             // Recompute every frame (Editor/Play)

    [Header("Light settings")]
    public Color lightColor = new Color(1f, 0.95f, 0.6f, 1f);
    [Range(0f, 5f)] public float intensity = 0.8f;
    [Tooltip("Soft edge for freeform shape (if supported by your URP version).")]
    [Range(0f, 5f)] public float falloff = 1.0f;
    [Tooltip("Z offset so the light sits slightly in front/behind its owner.")]
    public float zOffset = -0.1f;

    const string LightName = "__LayerHighlight2D__";

    void OnEnable() => Refresh();
    void Update() { if (autoRefresh) Refresh(); }

    [ContextMenu("Refresh Now")]
    public void Refresh()
    {
        var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        var processed = new HashSet<Transform>();

        // Build/update lights for all objects on target layers (one per topmost-in-layer ancestor)
        foreach (var t in all)
        {
            if (!IsInMask(t.gameObject.layer, targetLayers)) continue;

            var top = HighestAncestorInMask(t, targetLayers);
            if (!processed.Add(top)) continue; // already handled by an ancestor

            if (TryGetBounds2D(top, includeInactive, out var b))
                CreateOrUpdateLight(top, b);
            else
                RemoveLight(top); // nothing to bound
        }

        // Clean up lights for objects that no longer qualify
        foreach (var t in all)
        {
            var child = t.Find(LightName);
            if (child && !processed.Contains(t))
                DestroyImmediate(child.gameObject);
        }
    }

    static bool IsInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    static Transform HighestAncestorInMask(Transform t, LayerMask mask)
    {
        var cur = t;
        while (cur.parent && IsInMask(cur.parent.gameObject.layer, mask))
            cur = cur.parent;
        return cur;
    }

    static bool TryGetBounds2D(Transform root, bool includeInactive, out Bounds bounds)
    {
        bounds = default;
        var cols = root.GetComponentsInChildren<Collider2D>(includeInactive);
        if (cols == null || cols.Length == 0) return false;

        bool has = false;
        foreach (var c in cols)
        {
            if (!includeInactive && !c.enabled) continue;
            if (!has) { bounds = c.bounds; has = true; }
            else bounds.Encapsulate(c.bounds);
        }
        return has;
    }

    void CreateOrUpdateLight(Transform owner, Bounds worldBounds)
    {
        // Get or create the child light
        Transform child = owner.Find(LightName);
        if (!child)
        {
            var go = new GameObject(LightName);
            child = go.transform;
            child.SetParent(owner, true);
            go.AddComponent<Light2D>();
        }

        var l = child.GetComponent<Light2D>();

        // Position/identity
        var center = worldBounds.center;
        child.position = new Vector3(center.x, center.y, owner.position.z + zOffset);
        child.rotation = Quaternion.identity;
        child.localScale = Vector3.one;

        // Configure as Freeform square
        l.lightType = Light2D.LightType.Freeform;
        l.color = lightColor;
        l.intensity = intensity;

        var ext = (Vector2)worldBounds.extents;
        const float minSize = 0.05f; // avoid degenerate zero-sized shapes
        ext.x = Mathf.Max(ext.x, minSize);
        ext.y = Mathf.Max(ext.y, minSize);

        var path = new Vector3[4] {
            new Vector3(-ext.x, -ext.y, 0),
            new Vector3( ext.x, -ext.y, 0),
            new Vector3( ext.x,  ext.y, 0),
            new Vector3(-ext.x,  ext.y, 0),
        };

        // Prefer the public API; fall back to reflection for older URP versions
        if (!TrySetShapePath(l, path))
        {
            // If your URP is too old/new and this fails, consider upgrading URP.
            // (Keeping this silent so it still runs without crashing.)
        }

        // Optional: softer edge if the property exists in your URP build
        TrySetFloat(l, "shapeLightFalloffSize", falloff);
    }

    void RemoveLight(Transform owner)
    {
        var t = owner.Find(LightName);
        if (t) DestroyImmediate(t.gameObject);
    }

    static bool TrySetShapePath(Light2D light, Vector3[] path)
    {
        var m = typeof(Light2D).GetMethod("SetShapePath",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null) { m.Invoke(light, new object[] { path }); return true; }

        var p = typeof(Light2D).GetProperty("shapePath",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanWrite) { p.SetValue(light, path); return true; }

        return false;
    }

    static void TrySetFloat(Light2D light, string member, float value)
    {
        var prop = light.GetType().GetProperty(member,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanWrite) { prop.SetValue(light, value); return; }

        var field = light.GetType().GetField(member,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null) field.SetValue(light, value);
    }
}
