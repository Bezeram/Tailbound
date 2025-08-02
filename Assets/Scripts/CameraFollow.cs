using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraFollow : MonoBehaviour
{
    public Transform Target;
    public Screen Screen;
    private PixelPerfectCamera pixelPerfectCamera;

    void Start()
    {
        pixelPerfectCamera = GetComponent<PixelPerfectCamera>();

        if (pixelPerfectCamera == null)
        {
            Debug.LogError("PixelPerfectCamera component not found on this GameObject.");
        }
    }

    void Update()
    {
        if (Target == null)
        {
            Debug.LogWarning("Assign the target to the game object.");
            return;
        }

        Vector2 cameraPos = GetCameraPosition();
        transform.position = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
    }

    Vector2 GetCameraPosition()
    {
        // Convert target position to pixels
        float ppu = 16; // pixels per world unit
        Vector2 cameraPosition =  Target.position * ppu;

        // Half camera size in pixels
        float halfCamWidth = pixelPerfectCamera.refResolutionX / 2f;
        float halfCamHeight = pixelPerfectCamera.refResolutionY / 2f;

        // Screen bounds are in pixels (position & size are now treated as pixels)
        Vector2 screenMin = Screen.transform.position * ppu;
        Vector2 screenMax = screenMin + Screen.size;

        // Clamp X position in pixels
        if (cameraPosition.x - halfCamWidth < screenMin.x)
            cameraPosition.x = screenMin.x + halfCamWidth;
        else if (cameraPosition.x + halfCamWidth > screenMax.x)
            cameraPosition.x = screenMax.x - halfCamWidth;

        // Clamp Y position in pixels
        if (cameraPosition.y - halfCamHeight < screenMin.y)
            cameraPosition.y = screenMin.y + halfCamHeight;
        else if (cameraPosition.y + halfCamHeight > screenMax.y)
            cameraPosition.y = screenMax.y - halfCamHeight;

        // Convert back to world units
        return cameraPosition / ppu;
    }
}
