using UnityEngine;

public class PlayerScreenTransition : MonoBehaviour
{
    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning) return;

        ScreenTransitionScript trigger = other.GetComponent<ScreenTransitionScript>();
        if (trigger != null)
        {
            isTransitioning = true;
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                StartCoroutine(camFollow.ScreenTransition(trigger.newScreen));
            }
        }
    }
}
