using UnityEngine;

public static class Utils
{
    public static bool Compare_close_vectors(Vector3 first, Vector3 second, float error)
    {
        float diference_x = first.x - second.x;
        float diference_y = first.y - second.y;
        float diference_z = first.z - second.z;

        return diference_x < error && diference_x > -error &&
            diference_y < error && diference_y > -error &&
            diference_z < error && diference_z > -error;
    }

    public static bool IsInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    // Easing functions
    public static float EaseInCubic(float t) => t * t * t;
    public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    public static float EaseInOutCubic(float t) =>
        t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

    public static float EaseInQuad(float t) => t * t;
    public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    public static float EaseInOutQuad(float t) => 
        t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
}
