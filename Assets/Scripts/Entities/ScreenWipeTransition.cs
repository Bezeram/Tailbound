using System.Collections;
using UnityEngine;

/// <summary>
/// A directional screen-wipe transition: a solid panel slides in from one
/// edge until it fully covers the screen, then (on a later, separate call -
/// there's usually gameplay logic to run while the screen's covered, e.g.
/// LevelLoader repositioning the player) slides further out the opposite
/// edge to reveal whatever's behind it. Built from scratch specifically to
/// not depend on any pre-made asset: PlayIn/PlayOut just animate a
/// RectTransform's anchoredPosition via a coroutine - no baked Animation
/// clip, no Animator Controller state machine, unlike the wipe this replaces.
/// </summary>
public class ScreenWipeTransition : MonoBehaviour
{
    [Tooltip("The solid-color panel that slides across - should already be " +
             "sized to fully cover the screen when centered (anchoredPosition 0,0).")]
    [SerializeField] private RectTransform _Panel;

    [Tooltip("How long one slide (in, or out) takes, in seconds.")]
    [SerializeField] private float _Duration = 1f;

    [Tooltip("Purely cosmetic easing - constant speed is a straight diagonal line from (0,0) to (1,1).")]
    [SerializeField] private AnimationCurve _Ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform _ParentRect;
    private Coroutine _Running;

    private void Awake()
    {
        _ParentRect = (RectTransform)_Panel.parent;
    }

    /// <summary>Slides the panel in from off-screen (left) until it fully covers the screen.</summary>
    public void PlayIn() => Restart(fromOffscreenLeft: true);

    /// <summary>From fully covering the screen, slides the panel further out
    /// (past the right edge) to reveal whatever's behind it.</summary>
    public void PlayOut() => Restart(fromOffscreenLeft: false);

    private void Restart(bool fromOffscreenLeft)
    {
        if (_Running != null)
            StopCoroutine(_Running);
        _Running = StartCoroutine(SlideRoutine(fromOffscreenLeft));
    }

    private IEnumerator SlideRoutine(bool fromOffscreenLeft)
    {
        // Read every frame, not just once, in case the window/canvas gets
        // resized mid-slide - keeps the panel's start/end points honest
        // relative to whatever the screen width actually is right now.
        float startX = fromOffscreenLeft ? -_ParentRect.rect.width : 0f;
        float endX = fromOffscreenLeft ? 0f : _ParentRect.rect.width;

        _Panel.anchoredPosition = new Vector2(startX, 0f);

        float elapsed = 0f;
        while (elapsed < _Duration)
        {
            elapsed += Time.deltaTime;
            float eased = _Ease.Evaluate(Mathf.Clamp01(elapsed / _Duration));
            _Panel.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, eased), 0f);
            yield return null;
        }

        _Panel.anchoredPosition = new Vector2(endX, 0f);
        _Running = null;
    }
}
