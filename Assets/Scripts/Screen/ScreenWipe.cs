using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenWipe : MonoBehaviour
{
    public Image blackImage; // Fullscreen black panel
    public float duration = 0.5f; // fade time

    void Awake()
    {
        if (blackImage != null)
        {
            // Start fully transparent
            var c = blackImage.color;
            c.a = 0f;
            blackImage.color = c;
        }
    }

    // made this fade in/out transition for use in situations like level transitions, but it can be used for any screen wipe effect.
    // or we can use the banana_death_loading transtion for everything.

    public IEnumerator WipeIn()
    {
        float elapsed = 0f;
        //Color c = blackImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            //float t = Mathf.Clamp01(elapsed / duration);
            //c.a = t; // fade to opaque
            //blackImage.color = c;
            yield return null;
        }
    }

    public IEnumerator WipeOut()
    {
        float elapsed = 0f;
        //Color c = blackImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            //float t = Mathf.Clamp01(elapsed / duration);
            //c.a = 1f - t; // fade back to transparent
            //blackImage.color = c;
            yield return null;
        }
    }
}
