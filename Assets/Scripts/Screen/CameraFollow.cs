using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraFollow : MonoBehaviour
{
    public Transform Target;
    public ScreenBox Screen;
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

    public Vector2 GetCameraPosition()
    {
        float ppu = 16f; // pixels per world unit
        Camera cam = pixelPerfectCamera.GetComponent<Camera>();

        // Target position in pixels
        Vector2 cameraPosition = Target.position * ppu;

        // Get actual camera size in pixels
        float halfCamHeight = cam.orthographicSize * ppu;
        float halfCamWidth = halfCamHeight * cam.aspect;

        // Screen bounds in pixels
        Vector2 screenMin = Screen.transform.position * ppu;
        Vector2 screenMax = screenMin + (Screen.Size * ppu);

        // Clamp X position
        if (cameraPosition.x - halfCamWidth < screenMin.x)
            cameraPosition.x = screenMin.x + halfCamWidth;
        else if (cameraPosition.x + halfCamWidth > screenMax.x)
            cameraPosition.x = screenMax.x - halfCamWidth;

        // Clamp Y position
        if (cameraPosition.y - halfCamHeight < screenMin.y)
            cameraPosition.y = screenMin.y + halfCamHeight;
        else if (cameraPosition.y + halfCamHeight > screenMax.y)
            cameraPosition.y = screenMax.y - halfCamHeight;

        // Convert back to world units
        return cameraPosition / ppu;
    }
}
