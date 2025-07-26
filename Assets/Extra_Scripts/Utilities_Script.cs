using UnityEngine;

public static class Utilities_Script
{
    public static bool Compare_close_vectors(Vector3 first_vector, Vector3 second_vector)
    {
        float diference_x = first_vector.x - second_vector.x;
        float diference_y = first_vector.y - second_vector.y;
        float diference_z = first_vector.z - second_vector.z;
        if (diference_x < 0.1 && diference_x > -0.1 && diference_y < 0.1 && diference_y > -0.1 && diference_z < 0.1 && diference_z > -0.1)
            return true;
        return false;
    }
}
