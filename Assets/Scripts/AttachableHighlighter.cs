using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class LayerHighlight2D : MonoBehaviour
{
    [Header("Target objects")]
    public LayerMask targetLayers;
    public bool includeInactive = false;
    public bool autoRefresh = true;

    [Header("Distance-based highlight")]
    [Tooltip("Distance is measured from this Transform (defaults to this GameObject if null).")]
    public Transform rangeOrigin;
    [Min(0f)] public float minRange = 0f;     // full intensity at this distance
    [Min(0f)] public float maxRange = 20f;    // zero intensity at/after this distance
    [Tooltip("If true, objects closer than minRange or farther than maxRange are not lit at all.")]
    public bool hideOutsideRange = true;

    [Header("Light settings")]
    public Color lightColor = new Color(1f, 0.95f, 0.6f, 1f);
    [Range(0f, 30f)] public float BaseIntensity = 1f; // intensity at minRange
    [Range(0f, 5f)] public float falloff = 1.0f;      // soft edge if supported by your URP
    public float zOffset = -0.1f;

    const string LightName = "__LayerHighlight2D__";

    void OnEnable() => Refresh();
    void Update() { if (autoRefresh) Refresh(); }

    [ContextMenu("Refresh Now")]
    public void Refresh()
    {
        var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        var processed = new HashSet<Transform>();

        Vector3 origin = (rangeOrigin ? rangeOrigin : transform).position;
        Vector2 originXY = new Vector2(origin.x, origin.y);

        foreach (var t in all)
        {
            if (!IsInMask(t.gameObject.layer, targetLayers)) 
                continue;

            var top = HighestAncestorInMask(t, targetLayers);
            if (!processed.Add(top)) continue;

            if (!TryGetBounds2D(top, includeInactive, out var b))
            {
                RemoveLight(top);
                continue;
            }

            // Distance from origin to the bounds (edge) in XY
            var cp = b.ClosestPoint(new Vector3(origin.x, origin.y, b.center.z));
            float d = Vector2.Distance(originXY, new Vector2(cp.x, cp.y));

            float factor = ComputeIntensityFactor(d, minRange, maxRange, hideOutsideRange);

            if (factor <= 0f && hideOutsideRange)
            {
                RemoveLight(top);
                continue;
            }

            float lightStrength = BaseIntensity * factor;
            CreateOrUpdateLight(top, b, lightStrength);
        }

        // Cleanup: remove highlight objects from things no longer processed
        foreach (var t in all)
        {
            var child = t.Find(LightName);
            if (child && !processed.Contains(t))
                DestroyImmediate(child.gameObject);
        }
    }

    static float ComputeIntensityFactor(float d, float minR, float maxR, bool hideOutside)
    {
        if (maxR <= minR + 1e-4f)
        {
            // Degenerate case: treat as a hard cutoff at minR
            return (hideOutside ? (Mathf.Abs(d - minR) < 1e-4f ? 1f : 0f)
                                : (d <= minR ? 1f : 0f));
        }

        // 1 at minRange, 0 at/after maxRange (smooth)
        float t = 1f - Mathf.InverseLerp(minR, maxR, d);
        if (hideOutside && (d < minR || d > maxR)) return 0f;
        return Mathf.Clamp01(t);
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
            if (!includeInactive && (!c.enabled || !c.gameObject.activeInHierarchy)) continue;
            if (!has) { bounds = c.bounds; has = true; }
            else bounds.Encapsulate(c.bounds);
        }
        return has;
    }

    void CreateOrUpdateLight(Transform owner, Bounds worldBounds, float intensity)
    {
        Transform child = owner.Find(LightName);
        if (!child)
        {
            var go = new GameObject(LightName);
            child = go.transform;
            child.SetParent(owner, true);
            go.AddComponent<Light2D>();
        }

        var l = child.GetComponent<Light2D>();

        // Position in the center of the bounds
        var c = worldBounds.center;
        child.position = new Vector3(c.x, c.y, owner.position.z + zOffset);
        child.rotation = Quaternion.identity;
        child.localScale = Vector3.one;

        // Square freeform shape
        var ext = (Vector2)worldBounds.extents;
        const float minSize = 0.05f;
        ext.x = Mathf.Max(ext.x, minSize);
        ext.y = Mathf.Max(ext.y, minSize);

        l.lightType = Light2D.LightType.Freeform;
        l.color = lightColor;
        l.intensity = intensity;

        var path = new Vector3[4] {
            new Vector3(-ext.x, -ext.y, 0),
            new Vector3( ext.x, -ext.y, 0),
            new Vector3( ext.x,  ext.y, 0),
            new Vector3(-ext.x,  ext.y, 0),
        };

        TrySetShapePath(l, path);
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
