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
            var c = blackImage.color;
            c.a = 0f;
            blackImage.color = c;
        }
    }

    public IEnumerator WipeIn()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public IEnumerator WipeOut()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
