using UnityEngine;

public static class Utilities_Script
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
}
