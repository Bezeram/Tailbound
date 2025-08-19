using Unity.XR.OpenVR;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public float TimeScale1 = 1f;
    public float TimeScale2 = 0.1f;
    public float CurrentTimeScale = 1f;
    public bool IsPaused = false;
    public bool UsingTimeScale1 = true;

    void Pause()
    {
        IsPaused = true;
    }

    void Resume()
    {
        IsPaused = false;
    }

    void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            TogglePause();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            UsingTimeScale1 = !UsingTimeScale1;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            CurrentTimeScale += 0.25f;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            CurrentTimeScale -= 0.25f;
        }
    }

    void Update()
    {
        HandleInput();

        if (IsPaused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            // Choose between 2 different time scales.
            CurrentTimeScale = UsingTimeScale1 ? TimeScale1 : TimeScale2;

            Time.timeScale = CurrentTimeScale;
        }
    }
}
