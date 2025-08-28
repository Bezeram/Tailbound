using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    public ScreenArea CurrentScreen;
    public Vector3 CurrentSpawnPosition => CurrentScreen.CurrentSpawnPosition;

    void OnValidate()
    {
        if (CurrentScreen == null)
            Debug.LogError("No start screen selected!");
    }
}
